namespace GoogleDriveCLIManager.Services
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Drive.v3.Data;
    using Google.Apis.Services;
    using Google.Apis.Util.Store;
    using GoogleDriveCLIManager.Infrastructure.Exceptions;
    using GoogleDriveCLIManager.Services.Interfaces;
    using System.IO;

    /// <summary>
    /// Handles OAuth 2.0 authentication with Google Drive API.
    /// Persists the token locally so the user does not need to re-authenticate on every run.
    /// </summary>
    public class AuthService : IAuthService
    {
        private static readonly string[] Scopes = { DriveService.Scope.Drive };

        private const string ApplicationName = "GoogleDriveCLI";

        private const string CredentialsFileName = "client_secret.json";

        private static readonly string TokenStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".google-drive-cli", "token"
        );

        /// <summary>
        /// Authenticates the user via OAuth 2.0 and returns an authenticated DriveService.
        /// On first run, opens a browser for the user to log in and grant Drive access.
        /// On subsequent runs, uses the persisted token automatically.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the authentication flow.</param>
        /// <returns>An authenticated <see cref="DriveService"/> instance.</returns>
        /// <exception cref="FileNotFoundException">Thrown when client_secret.json is not found.</exception>
        /// <exception cref="DriveAuthenticationException">Thrown when authentication fails.</exception>
        public async Task<DriveService> GetAuthenticatedServiceAsync(
            CancellationToken cancellationToken = default)
        {

            try
            {
                var credentialsPath = FindCredentialsFile();

                UserCredential credential;

                await using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    user: "user",
                    cancellationToken,
                    new FileDataStore(TokenStorePath, fullPath: true)
                );

                return new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DriveAuthenticationException(
                    "Failed to authenticate with Google Drive. " +
                    "Please check your client_secret.json and try again.", ex);
            }
        }

        /// <summary>
        /// Locates the client_secret.json file by checking known locations.
        /// First checks the current working directory, then the application base directory.
        /// </summary>
        /// <returns>The full path to the client_secret.json file.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when client_secret.json cannot be found in any of the expected locations.
        /// Includes a descriptive message directing the user to the README for setup instructions.
        /// </exception>
        private static string FindCredentialsFile()
        {
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), CredentialsFileName);
            if (System.IO.File.Exists(localPath))
                return localPath;

            var execPath = Path.Combine(
                AppContext.BaseDirectory,
                CredentialsFileName
            );
            if (System.IO.File.Exists(execPath))
                return execPath;

            throw new FileNotFoundException(
                $"Could not find '{CredentialsFileName}'. " +
                $"Please place it in the application root directory. " +
                $"See README.md for setup instructions."
            );
        }
    }
}