namespace GoogleDriveCLI.Models
{

    using GoogleDriveCLIManager.Models;

    /// <summary>
    /// The JSON state file.
    /// </summary>
    public class SyncManifest
    {
        public DateTime LastSyncTime { get; set; }
        public Dictionary<string, ManifestEntry> Entries { get; set; } = new();
    }
}