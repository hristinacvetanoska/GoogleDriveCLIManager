namespace GoogleDriveCLIManager.Helpers
{
    /// <summary>
    /// Provides utility methods for working with MIME types in the context of Google Drive.
    /// Handles file type display, Google Workspace export MIME types and export extensions.
    /// </summary>
    public static class MimeTypeHelper
    {
        /// <summary>
        /// Returns a Spectre.Console formatted display string for a given MIME type.
        /// Includes an emoji icon and color markup for console rendering.
        /// </summary>
        /// <param name="mimeType">The MIME type string.</param>
        /// <returns>A Spectre.Console markup string representing the file type.</returns>
        public static string GetFileType(string mimeType) => mimeType switch
        {
            "application/vnd.google-apps.folder" => "[blue]📁 Folder[/]",
            "application/vnd.google-apps.document" => "[blue]📝 Google Doc[/]",
            "application/vnd.google-apps.spreadsheet" => "[green]📊 Google Sheet[/]",
            "application/vnd.google-apps.presentation" => "[yellow]📊 Google Slides[/]",
            "application/pdf" => "[red]📄 PDF[/]",
            "image/jpeg" or "image/png" or "image/gif" => "[magenta]🖼 Image[/]",
            "video/mp4" or "video/quicktime" => "[blue]🎥 Video[/]",
            "audio/mpeg" or "audio/wav" => "[blue]🎵 Audio[/]",
            "application/zip" => "[grey]🗜 Archive[/]",
            _ => "[grey]📄 File[/]"
        };

        /// <summary>
        /// Returns the export MIME type for Google Workspace files.
        /// Google Workspace files cannot be downloaded directly and must be exported
        /// to an equivalent Office format before downloading.
        /// </summary>
        /// <param name="mimeType">The Google Workspace MIME type.</param>
        /// <returns>
        /// The export MIME type string, or an empty string for non-Workspace files.
        /// Examples:
        /// "application/vnd.google-apps.document" → Word document MIME type
        /// "application/vnd.google-apps.spreadsheet" → Excel MIME type
        /// "application/pdf" → empty string (not a Workspace file)
        /// </returns>
        public static string GetExportMimeType(string mimeType) => mimeType switch
        {
            "application/vnd.google-apps.document" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.google-apps.spreadsheet" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.google-apps.presentation" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.google-apps.drawing" => "image/png",
            _ => string.Empty
        };

        /// <summary>
        /// Returns the file extension to append when exporting a Google Workspace file.
        /// Used to give the downloaded file the correct extension after export.
        /// </summary>
        /// <param name="mimeType">The Google Workspace MIME type.</param>
        /// <returns>
        /// The file extension string including the dot, or empty string for non-Workspace files.
        /// Examples:
        /// "application/vnd.google-apps.document" → ".docx"
        /// "application/vnd.google-apps.spreadsheet" → ".xlsx"
        /// "application/pdf" → ""
        /// </returns>
        public static string GetExportExtension(string mimeType) => mimeType switch
        {
            "application/vnd.google-apps.document" => ".docx",
            "application/vnd.google-apps.spreadsheet" => ".xlsx",
            "application/vnd.google-apps.presentation" => ".pptx",
            "application/vnd.google-apps.drawing" => ".png",
            _ => string.Empty
        };
    }
}