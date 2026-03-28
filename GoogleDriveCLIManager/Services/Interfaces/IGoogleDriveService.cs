namespace GoogleDriveCLIManager.Services.Interfaces
{
    using GoogleDriveCLIManager.Models;

    public interface IGoogleDriveService
    {
        Task<IList<DriveFileInfo>> ListAllFilesAsync(CancellationToken cancellationToken = default);
        Task<Stream> DownloadFileAsync(DriveFileInfo file, CancellationToken cancellationToken = default);
        Task<IList<DriveFileInfo>> SearchFilesAsync(string query, CancellationToken cancellationToken = default);
        Task<string> GetOrCreateFolderPathAsync(string drivePath, CancellationToken cancellationToken = default);
        Task<string> UploadFileAsync(string localPath, string driveFolderId, CancellationToken cancellationToken = default);
    }
}