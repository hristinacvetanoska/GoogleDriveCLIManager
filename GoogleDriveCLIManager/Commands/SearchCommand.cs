namespace GoogleDriveCLIManager.Commands
{
    using GoogleDriveCLIManager.Helpers;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using Spectre.Console;
    using Spectre.Console.Cli;
    using System.ComponentModel;


    /// <summary>
    /// Settings for the search command.
    /// </summary>
    public class SearchCommandSettings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the search term used to find files on Google Drive.
        /// </summary>
        [CommandArgument(0, "<query>")]
        [Description("The search term to find files on Google Drive")]
        public string Query { get; set; } = string.Empty;
    }

    /// <summary>
    /// Command that searches for files on Google Drive by name
    /// and displays results with their local sync status.
    /// </summary>
    public class SearchCommand : AsyncCommand<SearchCommandSettings>
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IManifestRepository _manifestRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchCommand"/> class with the specified dependencies.
        /// </summary>
        /// <param name="driveService">Service used to interact with Google Drive.</param>
        /// <param name="manifestRepository">Repository for accessing local sync state.</param>
        public SearchCommand(
            IGoogleDriveService driveService,
            IManifestRepository manifestRepository)
        {
            _driveService = driveService;
            _manifestRepository = manifestRepository;
        }

        /// <summary>
        /// Executes the search command.
        /// Queries the Google Drive API for files matching the search term,
        /// checks the local manifest for sync status of each result,
        /// and renders a formatted results table.
        /// </summary>
        /// <param name="context">The command context provided by Spectre.Console.Cli.</param>
        /// <param name="settings">The command settings containing the search query.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Exit code 0 on success.</returns>
        public override async Task<int> ExecuteAsync(
            CommandContext context,
            SearchCommandSettings settings,
            CancellationToken cancellationToken)
        {
            AnsiConsole.MarkupLine($"[bold blue]Searching Google Drive for:[/] [yellow]{settings.Query}[/]");
            AnsiConsole.WriteLine();

            var manifest = await _manifestRepository.LoadAsync();

            IList<DriveFileInfo> results = null!;
            await AnsiConsole.Status()
                .StartAsync("Searching Google Drive...", async ctx =>
                {
                    results = await _driveService.SearchFilesAsync(
                        settings.Query, cancellationToken);
                });

            if (results.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No files found matching your query.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]Found {results.Count} result(s):[/]");
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Blue)
                .Title("[bold yellow]Search Results[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Type[/]")
                .AddColumn("[bold]Size[/]")
                .AddColumn("[bold]Modified[/]")
                .AddColumn("[bold]Status[/]");

            foreach (var file in results)
            {
                var isDownloaded = manifest.Entries.ContainsKey(file.Id);

                var status = isDownloaded
                    ? "[green]✓ Synced[/]"
                    : "[yellow]✗ Not Downloaded[/]";

                var size = file.Size.HasValue
                    ? FileSizeFormatter.Format(file.Size.Value)
                    : "[grey]N/A[/]";

                var modified = file.ModifiedTime.HasValue
                    ? file.ModifiedTime.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                    : "[grey]Unknown[/]";

                var fileType = MimeTypeHelper.GetFileType(file.MimeType);

                table.AddRow(
                    MarkupHelper.Escape(file.Name),
                    fileType,
                    size,
                    modified,
                    status
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Tip: Run [white]sync[/] to download files marked as Not Downloaded.[/]");

            return 0;
        }
    }
}