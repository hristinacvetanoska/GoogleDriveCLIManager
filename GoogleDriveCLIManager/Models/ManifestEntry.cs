namespace GoogleDriveCLIManager.Models
{
    /// <summary>
    /// Represents a single downloaded file entry in the sync manifest.
    /// </summary>
    public class ManifestEntry
    {
        /// <summary>Gets the Google Drive file identifier.</summary>
        public string FileId { get; init; } = string.Empty;

        /// <summary>Gets the name of the file at the time it was downloaded.</summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>Gets the local file system path where the file was saved.</summary>
        public string LocalPath { get; init; } = string.Empty;

        /// <summary>Gets the UTC timestamp of when the file was downloaded.</summary>
        public DateTime DownloadedAt { get; init; }

        /// <summary>Gets the size of the downloaded file in bytes, if available.</summary>
        public long? FileSizeBytes { get; init; }
    }
}
