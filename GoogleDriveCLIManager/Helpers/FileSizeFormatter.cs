namespace GoogleDriveCLIManager.Helpers
{
    /// <summary>
    /// Provides utility methods for formatting file sizes into human-readable strings.
    /// </summary>
    public static class FileSizeFormatter
    {
        /// <summary>
        /// Formats a file size in bytes into a human-readable string.
        /// Automatically selects the appropriate unit (B, KB, MB, GB).
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>
        /// A formatted string representing the file size.
        /// Examples: "512 B", "1.00 KB", "2.45 MB", "1.00 GB"
        /// </returns>
        public static string Format(long bytes) => bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
            >= 1_024 => $"{bytes / 1_024.0:F2} KB",
            _ => $"{bytes} B"
        };
    }
}