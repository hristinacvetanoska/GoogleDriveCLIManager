namespace GoogleDriveCLIManager.Services.Interfaces
{
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Models;

    /// <summary>
    /// Defines operations for managing the local sync manifest.
    /// The manifest tracks which Drive files have been downloaded locally,
    /// enabling O(1) sync status lookup during search.
    /// </summary>
    public interface IManifestRepository
    {
        /// <summary>
        /// Loads the manifest from disk. Returns an empty manifest if none exists.
        /// Uses in-memory caching to avoid repeated disk reads.
        /// </summary>
        /// <returns>The current <see cref="SyncManifest"/>.</returns>
        Task<SyncManifest> LoadAsync();

        /// <summary>
        /// Persists the entire manifest to disk atomically.
        /// Thread-safe — uses SemaphoreSlim to prevent concurrent writes.
        /// </summary>
        /// <param name="manifest">The manifest to save.</param>
        Task SaveAsync(SyncManifest manifest);

        /// <summary>
        /// Adds or updates a single entry in the manifest and persists it.
        /// Thread-safe — uses SemaphoreSlim to prevent concurrent writes.
        /// </summary>
        /// <param name="entry">The manifest entry to add.</param>
        Task AddEntryAsync(ManifestEntry entry);

        /// <summary>
        /// Checks whether a file has been downloaded by looking up its Drive file ID
        /// in the cached manifest. O(1) lookup.
        /// </summary>
        /// <param name="fileId">The Google Drive file ID to check.</param>
        /// <returns>True if the file has been downloaded; otherwise false.</returns>
        bool IsDownloaded(string fileId);
    }
}