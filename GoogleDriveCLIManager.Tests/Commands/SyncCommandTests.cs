namespace GoogleDriveCLIManager.Tests.Commands
{
    using FluentAssertions;
    using GoogleDriveCLI.Models;
    using GoogleDriveCLIManager.Commands;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using Moq;
    using Spectre.Console;

    [Collection("Sequential")]
    public class SyncCommandTests
    {
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly Mock<IFileSystemService> _fileSystemServiceMock;
        private readonly Mock<IManifestRepository> _manifestRepositoryMock;
        private readonly SyncCommand _command;

        public SyncCommandTests()
        {
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _fileSystemServiceMock = new Mock<IFileSystemService>();
            _manifestRepositoryMock = new Mock<IManifestRepository>();

            _command = new SyncCommand(
                _driveServiceMock.Object,
                _fileSystemServiceMock.Object,
                _manifestRepositoryMock.Object);

            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(TextWriter.Null)
            });
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnZero_WhenNoFilesOnDrive()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DriveFileInfo>());

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            // Act
            var result = await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnZero_WhenAllFilesAlreadySynced()
        {
            // Arrange
            var manifest = new SyncManifest();
            manifest.Entries["fileId1"] = new ManifestEntry { FileId = "fileId1", FileName = "file1.pdf" };

            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DriveFileInfo>
                {
                new() { Id = "fileId1", Name = "file1.pdf", MimeType = "application/pdf", Size = 1024 }
                });

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(manifest);

            // Act
            var result = await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            result.Should().Be(0);

            _driveServiceMock.Verify(
                x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDownloadNewFiles_WhenNotInManifest()
        {
            // Arrange
            var files = new List<DriveFileInfo>
        {
            new() { Id = "fileId1", Name = "file1.pdf", MimeType = "application/pdf", Size = 1024 },
            new() { Id = "fileId2", Name = "file2.pdf", MimeType = "application/pdf", Size = 2048 }
        };

            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _driveServiceMock
                .Setup(x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());

            _fileSystemServiceMock
                .Setup(x => x.SanitizeFileName(It.IsAny<string>()))
                .Returns<string>(name => name);

            _fileSystemServiceMock
                .Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            _manifestRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<SyncManifest>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            result.Should().Be(0);

            _driveServiceMock.Verify(
                x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSaveManifest_AfterDownload()
        {
            // Arrange
            var files = new List<DriveFileInfo>
        {
            new() { Id = "fileId1", Name = "file1.pdf", MimeType = "application/pdf", Size = 1024 }
        };

            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _driveServiceMock
                .Setup(x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());

            _fileSystemServiceMock
                .Setup(x => x.SanitizeFileName(It.IsAny<string>()))
                .Returns<string>(name => name);

            _fileSystemServiceMock
                .Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            _manifestRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<SyncManifest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            _manifestRepositoryMock.Verify(
                x => x.SaveAsync(It.IsAny<SyncManifest>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldContinue_WhenOneFileFailsToDownload()
        {
            // Arrange
            var files = new List<DriveFileInfo>
        {
            new() { Id = "fileId1", Name = "file1.pdf", MimeType = "application/pdf", Size = 1024 },
            new() { Id = "fileId2", Name = "file2.pdf", MimeType = "application/pdf", Size = 2048 }
        };

            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _driveServiceMock
                .SetupSequence(x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Network error"))
                .ReturnsAsync(new MemoryStream());

            _fileSystemServiceMock
                .Setup(x => x.SanitizeFileName(It.IsAny<string>()))
                .Returns<string>(name => name);

            _fileSystemServiceMock
                .Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            _manifestRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<SyncManifest>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSkipAlreadyDownloadedFiles()
        {
            // Arrange
            var manifest = new SyncManifest();
            manifest.Entries["fileId1"] = new ManifestEntry { FileId = "fileId1" };
            manifest.Entries["fileId2"] = new ManifestEntry { FileId = "fileId2" };

            var files = new List<DriveFileInfo>
            {
                new() { Id = "fileId1", Name = "file1.pdf", MimeType = "application/pdf" },
                new() { Id = "fileId2", Name = "file2.pdf", MimeType = "application/pdf" },
                new() { Id = "fileId3", Name = "file3.pdf", MimeType = "application/pdf" }
            };

            _driveServiceMock
                .Setup(x => x.ListAllFilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _driveServiceMock
                .Setup(x => x.DownloadFileAsync(It.IsAny<DriveFileInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream());

            _fileSystemServiceMock
                .Setup(x => x.SanitizeFileName(It.IsAny<string>()))
                .Returns<string>(name => name);

            _fileSystemServiceMock
                .Setup(x => x.SaveFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(manifest);

            _manifestRepositoryMock
                .Setup(x => x.SaveAsync(It.IsAny<SyncManifest>()))
                .Returns(Task.CompletedTask);

            // Act
            await _command.ExecuteAsync(
                null!, new SyncCommandSettings(), CancellationToken.None);

            // Assert
            _driveServiceMock.Verify(
                x => x.DownloadFileAsync(
                    It.Is<DriveFileInfo>(f => f.Id == "fileId3"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _driveServiceMock.Verify(
                x => x.DownloadFileAsync(
                    It.Is<DriveFileInfo>(f => f.Id == "fileId1"),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}