namespace GoogleDriveCLIManager.Tests.Commands
{
    using FluentAssertions;
    using GoogleDriveCLIManager.Commands;
    using GoogleDriveCLIManager.Services.Interfaces;
    using Moq;
    using Spectre.Console;

    [Collection("Sequential")]
    public class UploadCommandTests
    {
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly UploadCommand _command;
        private readonly string _testFilePath;

        public UploadCommandTests()
        {
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _command = new UploadCommand(_driveServiceMock.Object);

            _testFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
            File.WriteAllText(_testFilePath, "test content");

            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(TextWriter.Null)
            });
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnOne_WhenFileDoesNotExist()
        {
            // Arrange
            var settings = new UploadCommandSettings
            {
                LocalPath = "C:\\nonexistent\\file.pdf",
                DrivePath = "root"
            };

            // Act
            var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallGetOrCreateFolderPath_WithCorrectPath()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.GetOrCreateFolderPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("folderId123");

            _driveServiceMock
                .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("uploadedFileId");

            var settings = new UploadCommandSettings
            {
                LocalPath = _testFilePath,
                DrivePath = "Work/Reports"
            };

            // Act
            await _command.ExecuteAsync(null!, settings, CancellationToken.None);

            // Assert
            _driveServiceMock.Verify(
                x => x.GetOrCreateFolderPathAsync("Work/Reports", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnZero_WhenUploadSucceeds()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.GetOrCreateFolderPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("folderId123");

            _driveServiceMock
                .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("uploadedFileId");

            var settings = new UploadCommandSettings
            {
                LocalPath = _testFilePath,
                DrivePath = "root"
            };

            // Act
            var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnOne_WhenUploadFails()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.GetOrCreateFolderPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("folderId123");

            _driveServiceMock
                .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network error"));

            var settings = new UploadCommandSettings
            {
                LocalPath = _testFilePath,
                DrivePath = "root"
            };

            // Act
            var result = await _command.ExecuteAsync(
                null!, settings, CancellationToken.None);

            // Assert
            result.Should().Be(1);
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }
    }
}