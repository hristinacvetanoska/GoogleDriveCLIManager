namespace GoogleDriveCLIManager.Services.Interfaces
{
    using Google.Apis.Drive.v3;

    /// <summary>
    /// Defines the contract for Google OAuth 2.0 authentication.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates the user with Google via OAuth 2.0 and returns
        /// an authenticated DriveService ready for API calls.
        /// On first run, opens a browser for user login.
        /// On subsequent runs, uses the persisted token automatically.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the authentication flow.</param>
        /// <returns>An authenticated <see cref="DriveService"/> instance.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when client_secret.json is not found in the expected location.
        /// </exception>
        /// <exception cref="DriveAuthenticationException">
        /// Thrown when authentication fails for any other reason.
        /// </exception>
        Task<DriveService> GetAuthenticatedServiceAsync(CancellationToken cancellationToken = default);
    }
}
