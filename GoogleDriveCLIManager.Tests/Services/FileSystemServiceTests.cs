namespace GoogleDriveCLIManager.Tests.Services
{
    using FluentAssertions;
    using GoogleDriveCLIManager.Services;

    public class FileSystemServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly FileSystemService _service;

        public FileSystemServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _service = new FileSystemService(_testDirectory);
        }

        [Fact]
        public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            // Act
            var result = _service.FileExists("nonexistent.pdf");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task SaveFileAsync_ThenFileExists_ShouldReturnTrue()
        {
            // Arrange
            var content = "Hello World"u8.ToArray();
            using var stream = new MemoryStream(content);

            // Act
            await _service.SaveFileAsync("test.txt", stream);
            var exists = _service.FileExists("test.txt");

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task SaveFileAsync_ShouldSaveCorrectContent()
        {
            // Arrange
            var expectedContent = "Test content 123"u8.ToArray();
            using var stream = new MemoryStream(expectedContent);

            // Act
            await _service.SaveFileAsync("content_test.txt", stream);

            // Assert
            var fullPath = Path.Combine(_testDirectory, "content_test.txt");
            var savedContent = await File.ReadAllBytesAsync(fullPath);
            savedContent.Should().BeEquivalentTo(expectedContent);
        }

        [Theory]
        [InlineData("file:name.pdf", "file_name.pdf")]
        [InlineData("file/name.pdf", "file_name.pdf")]
        [InlineData("file*name.pdf", "file_name.pdf")]
        [InlineData("  file.pdf  ", "file.pdf")]
        [InlineData("...file.pdf", "file.pdf")]
        public void SanitizeFileName_ShouldReplaceInvalidCharacters(
            string input, string expected)
        {
            // Act
            var result = _service.SanitizeFileName(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void EnsureDirectoryExists_ShouldCreateDirectory_WhenItDoesNotExist()
        {
            // Arrange
            var newDir = Path.Combine(_testDirectory, "newsubdir");

            // Act
            _service.EnsureDirectoryExists(newDir);

            // Assert
            Directory.Exists(newDir).Should().BeTrue();
        }

        [Fact]
        public void GetDownloadsPath_ShouldReturnCorrectPath()
        {
            // Act
            var path = _service.GetDownloadsPath();

            // Assert
            path.Should().Be(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }
    }
}