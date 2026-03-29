namespace GoogleDriveCLIManager.Tests.Infrastructure
{
    using FluentAssertions;
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Infrastructure;
    using GoogleDriveCLIManager.Models;

    public class ManifestRepositoryTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ManifestRepository _repository;

        public ManifestRepositoryTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            Environment.SetEnvironmentVariable("DOWNLOADS_PATH_OVERRIDE", _testDirectory);
            _repository = new ManifestRepository(_testDirectory);
        }

        [Fact]
        public async Task LoadAsync_ShouldReturnEmptyManifest_WhenFileDoesNotExist()
        {
            // Act
            var manifest = await _repository.LoadAsync();

            // Assert
            manifest.Should().NotBeNull();
            manifest.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveAsync_ThenLoadAsync_ShouldPersistManifest()
        {
            // Arrange
            var manifest = new SyncManifest();
            manifest.Entries["fileId123"] = new ManifestEntry
            {
                FileId = "fileId123",
                FileName = "test.pdf",
                LocalPath = "Downloads/test.pdf",
                DownloadedAt = DateTime.UtcNow,
                FileSizeBytes = 1024
            };

            // Act
            await _repository.SaveAsync(manifest);
            var loaded = await _repository.LoadAsync();

            // Assert
            loaded.Entries.Should().ContainKey("fileId123");
            loaded.Entries["fileId123"].FileName.Should().Be("test.pdf");
        }

        [Fact]
        public async Task AddEntryAsync_ShouldAddEntryToManifest()
        {
            // Arrange
            var entry = new ManifestEntry
            {
                FileId = "fileId456",
                FileName = "document.docx",
                LocalPath = "Downloads/document.docx",
                DownloadedAt = DateTime.UtcNow,
                FileSizeBytes = 2048
            };

            // Act
            await _repository.AddEntryAsync(entry);
            var manifest = await _repository.LoadAsync();

            // Assert
            manifest.Entries.Should().ContainKey("fileId456");
            manifest.Entries["fileId456"].FileName.Should().Be("document.docx");
        }

        [Fact]
        public async Task IsDownloaded_ShouldReturnTrue_WhenFileExistsInManifest()
        {
            // Arrange
            var manifest = new SyncManifest();
            manifest.Entries["fileId789"] = new ManifestEntry
            {
                FileId = "fileId789",
                FileName = "photo.jpg",
                LocalPath = "Downloads/photo.jpg",
                DownloadedAt = DateTime.UtcNow
            };
            await _repository.SaveAsync(manifest);
            await _repository.LoadAsync();

            // Act
            var result = _repository.IsDownloaded("fileId789");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsDownloaded_ShouldReturnFalse_WhenFileNotInManifest()
        {
            // Arrange
            await _repository.LoadAsync();

            // Act
            var result = _repository.IsDownloaded("nonExistentId");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddEntryAsync_CalledConcurrently_ShouldNotCorruptManifest()
        {
            // Arrange
            var entries = Enumerable.Range(1, 20).Select(i => new ManifestEntry
            {
                FileId = $"fileId{i}",
                FileName = $"file{i}.pdf",
                LocalPath = $"Downloads/file{i}.pdf",
                DownloadedAt = DateTime.UtcNow,
                FileSizeBytes = i * 1024
            }).ToList();

            // Act
            await Task.WhenAll(entries.Select(e => _repository.AddEntryAsync(e)));

            var manifest = await _repository.LoadAsync();

            // Assert
            manifest.Entries.Should().HaveCount(20);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }
    }
}