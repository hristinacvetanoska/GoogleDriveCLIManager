namespace GoogleDriveCLIManager.Models
{
    /// <summary>
    /// Represents a file/folder from Google Drive
    /// </summary>
    public class DriveFileInfo
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string MimeType { get; init; } = string.Empty;
        public long? Size { get; init; }
        public DateTime? ModifiedTime { get; init; }
        public string? ParentId { get; init; }
        public bool IsFolder => MimeType == "application/vnd.google-apps.folder";
        public bool IsGoogleWorkspaceFile => MimeType.StartsWith("application/vnd.google-apps");
    }
}
