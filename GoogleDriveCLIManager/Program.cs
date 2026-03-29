using Google.Apis.Drive.v3;
using GoogleDriveCLIManager.Commands;
using GoogleDriveCLIManager.Infrastructure;
using GoogleDriveCLIManager.Infrastructure.Exceptions;
using GoogleDriveCLIManager.Services;
using GoogleDriveCLIManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<IAuthService, AuthService>();
var authService = new AuthService();
DriveService driveService;

try
{
    driveService = await authService.GetAuthenticatedServiceAsync();
}
catch (FileNotFoundException ex)
{
    AnsiConsole.MarkupLine("[red]✗ Setup Error[/]");
    AnsiConsole.MarkupLine($"[yellow]{ex.Message}[/]");
    AnsiConsole.MarkupLine("[grey]See README.md for setup instructions.[/]");
    return 1;
}
catch (DriveAuthenticationException ex)
{
    AnsiConsole.MarkupLine("[red]✗ Authentication Failed[/]");
    AnsiConsole.MarkupLine($"[yellow]{ex.Message}[/]");
    return 1;
}
catch (OperationCanceledException)
{
    AnsiConsole.MarkupLine("[yellow]Authentication cancelled by user.[/]");
    return 1;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine("[red]✗ Unexpected Error During Authentication[/]");
    AnsiConsole.MarkupLine($"[yellow]{ex.Message}[/]");
    return 1;
}

services.AddSingleton(driveService);
services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<IManifestRepository, ManifestRepository>();

services.AddSingleton<SyncCommand>();
services.AddSingleton<SearchCommand>();
services.AddSingleton<UploadCommand>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("gdrive");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<SyncCommand>("sync")
        .WithDescription("Download all files from Google Drive to local Downloads folder");

    config.AddCommand<SearchCommand>("search")
        .WithDescription("Search for files on Google Drive by name");

    config.AddCommand<UploadCommand>("upload")
        .WithDescription("Upload a local file to a specific Google Drive folder path");

    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        return 1;
    });

#if DEBUG
    config.ValidateExamples();
#endif
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Unexpected error:[/] {ex.Message}");
    return 1;
}