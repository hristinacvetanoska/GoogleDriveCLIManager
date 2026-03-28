namespace GoogleDriveCLIManager.Services.Interfaces
{
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Models;

    public interface IManifestRepository
    {
        Task<SyncManifest> LoadAsync();
        Task SaveAsync(SyncManifest manifest);
        Task AddEntryAsync(ManifestEntry entry);
        bool IsDownloaded(string fileId);
    }
}