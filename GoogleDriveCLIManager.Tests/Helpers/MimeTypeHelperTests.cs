namespace GoogleDriveCLIManager.Tests.Helpers
{
    using FluentAssertions;
    using GoogleDriveCLIManager.Helpers;

    public class MimeTypeHelperTests
    {
        [Theory]
        [InlineData("application/vnd.google-apps.document", ".docx")]
        [InlineData("application/vnd.google-apps.spreadsheet", ".xlsx")]
        [InlineData("application/vnd.google-apps.presentation", ".pptx")]
        [InlineData("application/vnd.google-apps.drawing", ".png")]
        [InlineData("application/pdf", "")]
        [InlineData("image/jpeg", "")]
        public void GetExportExtension_ShouldReturnCorrectExtension(
            string mimeType, string expected)
        {
            // Act
            var result = MimeTypeHelper.GetExportExtension(mimeType);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("application/vnd.google-apps.document")]
        [InlineData("application/vnd.google-apps.spreadsheet")]
        [InlineData("application/vnd.google-apps.presentation")]
        [InlineData("application/vnd.google-apps.drawing")]
        public void GetExportMimeType_ShouldReturnNonEmpty_ForGoogleWorkspaceFiles(
            string mimeType)
        {
            // Act
            var result = MimeTypeHelper.GetExportMimeType(mimeType);

            // Assert
            result.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("image/jpeg")]
        [InlineData("video/mp4")]
        public void GetExportMimeType_ShouldReturnEmpty_ForRegularFiles(
            string mimeType)
        {
            // Act
            var result = MimeTypeHelper.GetExportMimeType(mimeType);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetFileType_ShouldReturnFolder_ForFolderMimeType()
        {
            // Act
            var result = MimeTypeHelper.GetFileType("application/vnd.google-apps.folder");

            // Assert
            result.Should().Contain("Folder");
        }

        [Fact]
        public void GetFileType_ShouldReturnFile_ForUnknownMimeType()
        {
            // Act
            var result = MimeTypeHelper.GetFileType("application/unknown");

            // Assert
            result.Should().Contain("File");
        }
    }
}