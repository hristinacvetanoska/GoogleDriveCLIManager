namespace GoogleDriveCLIManager.Models
{
    /// <summary>
    /// Holds all counters updated during parallel sync
    /// </summary>
    public class SyncStatistics
    {
        public int TotalFiles { get; set; }
        public int SuccessfulDownloads { get; set; }
        public int FailedDownloads { get; set; }
        public int SkippedFiles { get; set; }
        public long TotalBytesDownloaded { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public string FormattedSize => TotalBytesDownloaded switch
        {
            >= 1_073_741_824 => $"{TotalBytesDownloaded / 1_073_741_824.0:F2} GB",
            >= 1_048_576 => $"{TotalBytesDownloaded / 1_048_576.0:F2} MB",
            >= 1_024 => $"{TotalBytesDownloaded / 1_024.0:F2} KB",
            _ => $"{TotalBytesDownloaded} B"
        };
    }
}