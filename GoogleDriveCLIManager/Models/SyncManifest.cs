namespace GoogleDriveCLI.Models
{

    using GoogleDriveCLIManager.Models;

    /// <summary>
    /// The JSON state file.
    /// </summary>
    public class SyncManifest
    {
        /// <summary>
        /// Gets or sets the timestamp of the last successful sync operation (UTC).
        /// </summary>
        public DateTime LastSyncTime { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of downloaded files.
        /// Key is the Google Drive file ID for O(1) lookup.
        /// Value contains local file metadata.
        /// </summary
        public Dictionary<string, ManifestEntry> Entries { get; set; } = new();
    }
}