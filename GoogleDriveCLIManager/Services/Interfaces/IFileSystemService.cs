namespace GoogleDriveCLIManager.Services.Interfaces
{
    public interface IFileSystemService
    {
        Task SaveFileAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
        bool FileExists(string relativePath);
        string GetDownloadsPath();
        string SanitizeFileName(string fileName);
        void EnsureDirectoryExists(string path);
    }
}