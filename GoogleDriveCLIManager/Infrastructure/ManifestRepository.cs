namespace GoogleDriveCLIManager.Infrastructure
{
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using System.Text.Json;

    public class ManifestRepository : IManifestRepository
    {
        private readonly string _manifestPath;
        private SyncManifest? _cachedManifest;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

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

        public bool IsDownloaded(string fileId)
            => _cachedManifest?.Entries.ContainsKey(fileId) ?? false;
    }
}