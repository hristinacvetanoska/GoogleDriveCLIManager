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
    public class SearchCommandTests
    {
        private readonly Mock<IGoogleDriveService> _driveServiceMock;
        private readonly Mock<IManifestRepository> _manifestRepositoryMock;
        private readonly SearchCommand _command;

        public SearchCommandTests()
        {
            _driveServiceMock = new Mock<IGoogleDriveService>();
            _manifestRepositoryMock = new Mock<IManifestRepository>();
            _command = new SearchCommand(
                _driveServiceMock.Object,
                _manifestRepositoryMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnZero_WhenSearchReturnsResults()
        {
            // Arrange
            var files = new List<DriveFileInfo>
        {
            new() { Id = "1", Name = "report.pdf", MimeType = "application/pdf", Size = 1024 }
        };

            var manifest = new SyncManifest();
            manifest.Entries["1"] = new ManifestEntry { FileId = "1", FileName = "report.pdf" };

            _driveServiceMock
                .Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(manifest);

            // Act
            var result = await _command.ExecuteAsync(null!, new SearchCommandSettings { Query = "report" }, CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnZero_WhenNoFilesFound()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DriveFileInfo>());

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(TextWriter.Null)
            });

            // Act
            var result = await _command.ExecuteAsync(
                null!,
                new SearchCommandSettings { Query = "nothing" },
                CancellationToken.None);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallSearchFilesAsync_WithCorrectQuery()
        {
            // Arrange
            _driveServiceMock
                .Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DriveFileInfo>());

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            // Act
            await _command.ExecuteAsync(null!, new SearchCommandSettings { Query = "myquery" }, CancellationToken.None);

            // Assert
            _driveServiceMock.Verify(
                x => x.SearchFilesAsync("myquery", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCheckManifest_ForSyncStatus()
        {
            // Arrange
            var files = new List<DriveFileInfo>
            {
                new() { Id = "1", Name = "report.pdf", MimeType = "application/pdf" }
            };

            _driveServiceMock
                .Setup(x => x.SearchFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(files);

            _manifestRepositoryMock
                .Setup(x => x.LoadAsync())
                .ReturnsAsync(new SyncManifest());

            // Act
            await _command.ExecuteAsync(
                null!, new SearchCommandSettings { Query = "report" }, CancellationToken.None);

            // Assert
            _manifestRepositoryMock.Verify(x => x.LoadAsync(), Times.Once);
        }
    }
}