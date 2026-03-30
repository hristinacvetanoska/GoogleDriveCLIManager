namespace GoogleDriveCLIManager.Models
{
    /// <summary>
    /// Represents a file or folder retrieved from Google Drive.
    /// Maps from the Google SDK File object to a clean domain model.
    /// </summary>
    public class DriveFileInfo
    {
        /// <summary>Gets the unique Google Drive file identifier.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Gets the name associated with the object.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets the MIME type of the file.</summary>
        public string MimeType { get; init; } = string.Empty;

        /// <summary>Gets the size of the file in bytes. Null for Google Workspace files.</summary>
        public long? Size { get; init; }

        /// <summary>Gets the last modified timestampt in UTC.</summary>
        public DateTime? ModifiedTime { get; init; }

        /// <summary>Gets the ID of the parent folder. Null for root-level items.</summary>
        public string? ParentId { get; init; }

        /// <summary>
        /// Gets a value indicating whether the item represents a folder.
        /// </summary>
        public bool IsFolder => MimeType == "application/vnd.google-apps.folder";

        /// <summary>/// Gets a value indicating whether the file is a Google Workspace file.</summary>
        public bool IsGoogleWorkspaceFile => MimeType.StartsWith("application/vnd.google-apps");
    }
}
