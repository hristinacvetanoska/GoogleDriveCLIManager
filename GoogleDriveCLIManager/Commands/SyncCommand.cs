namespace GoogleDriveCLIManager.Commands
{
    using GoogleDriveCLIManager.Helpers;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks.Dataflow;

    public class SyncCommandSettings : CommandSettings { }

    public class SyncCommand : AsyncCommand<SyncCommandSettings>
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IFileSystemService _fileSystemService;
        private readonly IManifestRepository _manifestRepository;

        private int _successCount;
        private int _failureCount;
        private int _skippedCount;
        private long _totalBytesDownloaded;

        private readonly ConcurrentBag<string> _failedFiles = new();

        public SyncCommand(
            IGoogleDriveService driveService,
            IFileSystemService fileSystemService,
            IManifestRepository manifestRepository)
        {
            _driveService = driveService;
            _fileSystemService = fileSystemService;
            _manifestRepository = manifestRepository;
        }

        public override async Task<int> ExecuteAsync(
            CommandContext context,
            SyncCommandSettings settings,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            AnsiConsole.MarkupLine("[bold blue]Starting Google Drive sync...[/]");

            var manifest = await _manifestRepository.LoadAsync();

            IList<DriveFileInfo> files = null!;
            await AnsiConsole.Status()
                .StartAsync("Fetching file list from Google Drive...", async ctx =>
                {
                    files = await _driveService.ListAllFilesAsync(cancellationToken);
                });

            AnsiConsole.MarkupLine($"[green]Found {files.Count} files on Google Drive.[/]");

            var filesToDownload = files
                .Where(f => !manifest.Entries.ContainsKey(f.Id))
                .ToList();

            _skippedCount = files.Count - filesToDownload.Count;

            if (filesToDownload.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]All files already synced. Nothing to download.[/]");
                DisplayStatistics(stopwatch.Elapsed, files.Count);
                return 0;
            }

            AnsiConsole.MarkupLine($"[blue]Downloading {filesToDownload.Count} new files...[/]");

            await AnsiConsole.Progress()
                .AutoRefresh(true)
                .StartAsync(async ctx =>
                {
                    var progressTask = ctx.AddTask(
                        "[green]Downloading files[/]",
                        maxValue: filesToDownload.Count);

                    var downloadBlock = new ActionBlock<DriveFileInfo>(
                        async file => await DownloadFileAsync(file, manifest, progressTask, cancellationToken),
                        new ExecutionDataflowBlockOptions
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                            CancellationToken = cancellationToken
                        });

                    foreach (var file in filesToDownload)
                        downloadBlock.Post(file);

                    downloadBlock.Complete();
                    await downloadBlock.Completion;
                });

            await _manifestRepository.SaveAsync(manifest);

            stopwatch.Stop();

            DisplayStatistics(stopwatch.Elapsed, files.Count);

            return 0;
        }

        private async Task DownloadFileAsync(
            DriveFileInfo file,
            GoogleDriveCLI.Models.SyncManifest manifest,
            ProgressTask progressTask,
    CancellationToken cancellationToken)
        {
            try
            {
                var sanitizedName = _fileSystemService.SanitizeFileName(file.Name);

                if (file.IsGoogleWorkspaceFile)
                    sanitizedName += MimeTypeHelper.GetExportExtension(file.MimeType);

                await using var stream = await _driveService.DownloadFileAsync(file);
                await _fileSystemService.SaveFileAsync(sanitizedName, stream);

                var fileSize = file.Size ?? 0;
                Interlocked.Add(ref _totalBytesDownloaded, fileSize);
                Interlocked.Increment(ref _successCount);

                lock (manifest.Entries)
                {
                    manifest.Entries[file.Id] = new ManifestEntry
                    {
                        FileId = file.Id,
                        FileName = sanitizedName,
                        LocalPath = Path.Combine("Downloads", sanitizedName),
                        DownloadedAt = DateTime.UtcNow,
                        FileSizeBytes = file.Size
                    };
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failureCount);
                _failedFiles.Add($"{file.Name}: {ex.Message}");
            }
            finally
            {
                progressTask.Increment(1);
            }
        }

        private void DisplayStatistics(TimeSpan elapsed, int totalOnDrive)
        {
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Title("[bold yellow]Sync Statistics[/]")
                .AddColumn("[bold]Metric[/]")
                .AddColumn("[bold]Value[/]");

            table.AddRow("Total files on Drive", $"{totalOnDrive}");
            table.AddRow("[green]Successfully downloaded[/]", $"[green]{_successCount}[/]");
            table.AddRow("[yellow]Skipped (already synced)[/]", $"[yellow]{_skippedCount}[/]");
            table.AddRow("[red]Failed[/]", $"[red]{_failureCount}[/]");
            table.AddRow("Total data downloaded", FileSizeFormatter.Format(_totalBytesDownloaded));
            table.AddRow("Time elapsed", $"{elapsed:mm\\:ss\\.ff}");

            AnsiConsole.Write(table);

            if (!_failedFiles.IsEmpty)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]Failed files:[/]");
                foreach (var failed in _failedFiles)
                    AnsiConsole.MarkupLine($"  [red]✗[/] {failed}");
            }
        }
    }
}