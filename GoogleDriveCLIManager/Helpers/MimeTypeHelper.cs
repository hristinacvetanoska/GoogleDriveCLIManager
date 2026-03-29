namespace GoogleDriveCLIManager.Helpers
{
    public static class MimeTypeHelper
    {
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

        public static string GetExportMimeType(string mimeType) => mimeType switch
        {
            "application/vnd.google-apps.document" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.google-apps.spreadsheet" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.google-apps.presentation" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.google-apps.drawing" => "image/png",
            _ => string.Empty
        };

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