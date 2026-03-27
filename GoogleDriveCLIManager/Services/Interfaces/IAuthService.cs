namespace GoogleDriveCLIManager.Services.Interfaces
{
    using Google.Apis.Drive.v3;

    public interface IAuthService
    {
        Task<DriveService> GetAuthenticatedServiceAsync(CancellationToken cancellationToken = default);
    }
}
