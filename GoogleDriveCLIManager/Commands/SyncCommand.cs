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

    /// <summary>
    /// Settings for the sync command.
    /// No additional arguments or options are required —
    /// sync automatically downloads all files from Google Drive.
    /// </summary>
    public class SyncCommandSettings : CommandSettings { }

    /// <summary>
    /// Command that downloads all files from the authenticated user's Google Drive
    /// to a local Downloads directory using parallel processing.
    /// Uses ActionBlock for bounded parallelism and Interlocked for thread-safe statistics.
    /// </summary>
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

        /// <summary>
        /// Executes the sync command.
        /// Fetches all Drive files, filters already downloaded ones via manifest,
        /// downloads new files in parallel and displays statistics on completion.
        /// </summary>
        /// <param name="context">The command context provided by Spectre.Console.Cli.</param>
        /// <param name="settings">The command settings.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Exit code 0 on success.</returns>
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

        /// <summary>
        /// Downloads a single file from Google Drive and saves it locally.
        /// Updates thread-safe counters and manifest entry on success.
        /// Catches and logs exceptions without interrupting other parallel downloads.
        /// </summary>
        /// <param name="file">The Drive file to download.</param>
        /// <param name="manifest">The sync manifest to update after successful download.</param>
        /// <param name="progressTask">The Spectre.Console progress bar task to increment.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
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

        /// <summary>
        /// Renders a formatted statistics table to the console after sync completes.
        /// Shows total files, successful downloads, skipped files, failures,
        /// total data downloaded and elapsed time.
        /// </summary>
        /// <param name="elapsed">The total time taken for the sync operation.</param>
        /// <param name="totalOnDrive">The total number of files found on Google Drive.</param>
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