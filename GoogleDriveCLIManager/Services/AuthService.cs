namespace GoogleDriveCLIManager.Services
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Drive.v3.Data;
    using Google.Apis.Services;
    using Google.Apis.Util.Store;
    using GoogleDriveCLIManager.Services.Interfaces;
    using System.IO;

    public class AuthService : IAuthService
    {
        private static readonly string[] Scopes = { DriveService.Scope.Drive };

        private const string ApplicationName = "GoogleDriveCLI";

        private const string CredentialsFileName = "client_secret.json";

        private static readonly string TokenStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".google-drive-cli", "token"
        );

        public async Task<DriveService> GetAuthenticatedServiceAsync(
            CancellationToken cancellationToken = default)
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