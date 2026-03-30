namespace GoogleDriveCLIManager.Commands
{
    using GoogleDriveCLIManager.Helpers;
    using GoogleDriveCLIManager.Services.Interfaces;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;

    /// <summary>
    /// Settings for the upload command.
    /// </summary>
    public class UploadCommandSettings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the full local file system path of the file to upload.
        /// </summary>
        [CommandArgument(0, "<local_path>")]
        [Description("Path to the local file you want to upload")]
        public string LocalPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target folder path on Google Drive.
        /// If not specified, the file is uploaded directly to My Drive root.
        /// Nested paths are supported (e.g. "Work/Reports/2024").
        /// Non-existent folders are created automatically.
        /// </summary>
        [CommandArgument(1, "[drive_path]")]
        [Description("Target folder path on Google Drive. If not specified, uploads to My Drive root")]
        public string DrivePath { get; set; } = "root";
    }

    /// <summary>
    /// Command that uploads a local file to a specific folder path on Google Drive.
    /// Creates the target folder path automatically if it does not exist.
    /// </summary>
    public class UploadCommand : AsyncCommand<UploadCommandSettings>
    {
        private readonly IGoogleDriveService _driveService;

        public UploadCommand(IGoogleDriveService driveService)
        {
            _driveService = driveService;
        }

        /// <summary>
        /// Executes the upload command.
        /// Validates the local file exists, resolves or creates the target Drive folder,
        /// uploads the file with a progress bar and displays a summary on completion.
        /// </summary>
        /// <param name="context">The command context provided by Spectre.Console.Cli.</param>
        /// <param name="settings">The command settings containing local path and Drive path.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Exit code 0 on success, 1 on failure.</returns>
        public override async Task<int> ExecuteAsync(
            CommandContext context,
            UploadCommandSettings settings,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(settings.LocalPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{settings.LocalPath}[/]");
                AnsiConsole.MarkupLine("[grey]Please provide a valid local file path.[/]");
                return 1;
            }

            var fileName = Path.GetFileName(settings.LocalPath);
            var fileSize = new FileInfo(settings.LocalPath).Length;

            AnsiConsole.MarkupLine($"[bold blue]Uploading file:[/] [yellow]{fileName}[/]");
            AnsiConsole.MarkupLine($"[bold blue]Destination:[/] [yellow]{settings.DrivePath}[/]");
            AnsiConsole.MarkupLine($"[bold blue]Size:[/] [yellow]{FileSizeFormatter.Format(fileSize)}[/]");
            AnsiConsole.WriteLine();

            try
            {
                string folderId = string.Empty;

                await AnsiConsole.Status()
                    .StartAsync("Preparing destination folder on Google Drive...", async ctx =>
                    {
                        folderId = await _driveService.GetOrCreateFolderPathAsync(
                            settings.DrivePath, cancellationToken);
                    });

                AnsiConsole.MarkupLine($"[green]✓ Destination folder ready.[/]");

                string uploadedFileId = string.Empty;

                await AnsiConsole.Progress()
                    .AutoRefresh(true)
                    .StartAsync(async ctx =>
                    {
                        var progressTask = ctx.AddTask(
                            $"[green]Uploading {fileName}[/]",
                            maxValue: 100);

                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                        var progressSimulator = SimulateProgressAsync(progressTask, cts.Token);

                        uploadedFileId = await _driveService.UploadFileAsync(
                            settings.LocalPath, folderId, cancellationToken);

                        await cts.CancelAsync();
                        progressTask.Value = 100;

                        await progressSimulator;
                    });

                AnsiConsole.WriteLine();

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Title("[bold green]Upload Successful[/]")
                    .AddColumn("[bold]Detail[/]")
                    .AddColumn("[bold]Value[/]");

                table.AddRow("File name", fileName);
                table.AddRow("Drive path", settings.DrivePath);
                table.AddRow("File size", FileSizeFormatter.Format(fileSize));
                table.AddRow("Drive file ID", uploadedFileId);

                AnsiConsole.Write(table);

                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]Upload failed:[/] {ex.Message}");
                AnsiConsole.MarkupLine("[grey]Please check your connection and try again.[/]");
                return 1;
            }
        }

        /// <summary>
        /// Simulates upload progress in the console progress bar.
        /// Gradually fills the bar to 90% while the upload runs in the background.
        /// The remaining 10% completes when the upload finishes.
        /// </summary>
        /// <param name="progressTask">The Spectre.Console progress bar task to update.</param>
        /// <param name="cancellationToken">Token to stop the simulation when upload completes.</param>
        private static async Task SimulateProgressAsync(
            ProgressTask progressTask,
            CancellationToken cancellationToken)
        {
            try
            {
                while (progressTask.Value < 90 && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(300, cancellationToken);
                    progressTask.Increment(2);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}