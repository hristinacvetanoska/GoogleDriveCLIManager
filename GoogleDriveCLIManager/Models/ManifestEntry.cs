namespace GoogleDriveCLIManager.Models
{
    public class ManifestEntry
    {
        public string FileId { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string LocalPath { get; init; } = string.Empty;
        public DateTime DownloadedAt { get; init; }
        public long? FileSizeBytes { get; init; }
    }
}
