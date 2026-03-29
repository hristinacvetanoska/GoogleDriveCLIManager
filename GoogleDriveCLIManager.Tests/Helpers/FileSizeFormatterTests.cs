namespace GoogleDriveCLIManager.Tests.Helpers
{
    using FluentAssertions;
    using GoogleDriveCLIManager.Helpers;

    public class FileSizeFormatterTests
    {
        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(512, "512 B")]
        [InlineData(1_024, "1.00 KB")]
        [InlineData(1_048_576, "1.00 MB")]
        [InlineData(1_073_741_824, "1.00 GB")]
        [InlineData(2_048, "2.00 KB")]
        [InlineData(5_242_880, "5.00 MB")]
        public void Format_ShouldReturnCorrectSize(long bytes, string expected)
        {
            // Act
            var result = FileSizeFormatter.Format(bytes);

            // Assert
            result.Should().Be(expected);
        }
    }
}