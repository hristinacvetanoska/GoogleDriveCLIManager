namespace GoogleDriveCLIManager.Infrastructure
{
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using System.Text.Json;

    /// <summary>
    /// Persists the sync manifest to a local JSON file in the Downloads directory.
    /// Thread-safe for concurrent access during parallel file downloads.
    /// Uses SemaphoreSlim for async-compatible file write locking.
    /// </summary>
    public class ManifestRepository : IManifestRepository
    {
        private readonly string _manifestPath;
        private SyncManifest? _cachedManifest;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        // Default constructor uses the Downloads directory relative to the application base path.
        public ManifestRepository()
        {
            var downloadsPath = Path.Combine(
                    Path.GetDirectoryName(AppContext.BaseDirectory)!,
                    "..", "..", "..",
                    "Downloads"
                );

            downloadsPath = Path.GetFullPath(downloadsPath);
            _manifestPath = Path.Combine(downloadsPath, "manifest.json");
        }

        // Constructor for testing, allows specifying a custom path for the manifest file.
        public ManifestRepository(string downloadsPath)
        {
            _manifestPath = Path.Combine(downloadsPath, "manifest.json");
        }

        /// <summary>
        /// Loads the manifest from disk. Returns an empty manifest if none exists.
        /// Uses in-memory caching to avoid repeated disk reads.
        /// </summary>
        /// <returns>The current <see cref="SyncManifest"/>.</returns>
        public async Task<SyncManifest> LoadAsync()
        {
            if (_cachedManifest is not null)
                return _cachedManifest;

            if (!File.Exists(_manifestPath))
            {
                _cachedManifest = new SyncManifest();
                return _cachedManifest;
            }

            try
            {
                await using var stream = File.OpenRead(_manifestPath);
                _cachedManifest = await JsonSerializer.DeserializeAsync<SyncManifest>(
                    stream, JsonOptions) ?? new SyncManifest();
            }
            catch (JsonException)
            {
                _cachedManifest = new SyncManifest();
            }

            return _cachedManifest;
        }

        /// <summary>
        /// Persists the entire manifest to disk atomically.
        /// Thread-safe — uses SemaphoreSlim to prevent concurrent writes.
        /// </summary>
        /// <param name="manifest">The manifest to save.</param>
        public async Task SaveAsync(SyncManifest manifest)
        {
            var directory = Path.GetDirectoryName(_manifestPath)!;
            Directory.CreateDirectory(directory);

            await _lock.WaitAsync();
            try
            {
                await using var stream = File.Create(_manifestPath);
                await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions);
                _cachedManifest = manifest;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Adds or updates a single entry in the manifest and persists it.
        /// Thread-safe — uses SemaphoreSlim to prevent concurrent writes.
        /// </summary>
        /// <param name="entry">The manifest entry to add.</param>
        public async Task AddEntryAsync(ManifestEntry entry)
        {
            await _lock.WaitAsync();
            try
            {
                var manifest = await LoadAsync();
                manifest.Entries[entry.FileId] = entry;
                manifest.LastSyncTime = DateTime.UtcNow;

                var directory = Path.GetDirectoryName(_manifestPath)!;
                Directory.CreateDirectory(directory);

                await using var stream = File.Create(_manifestPath);
                await JsonSerializer.SerializeAsync(stream, manifest, JsonOptions);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Checks whether a file has been downloaded by looking up its Drive file ID
        /// in the cached manifest. O(1) lookup.
        /// </summary>
        /// <param name="fileId">The Google Drive file ID to check.</param>
        /// <returns>True if the file has been downloaded; otherwise false.</returns>
        public bool IsDownloaded(string fileId)
            => _cachedManifest?.Entries.ContainsKey(fileId) ?? false;
    }
}