namespace GoogleDriveCLIManager.Services.Interfaces
{
    using GoogleDriveCLIManager.Models;

    /// <summary>
    /// Defines operations for interacting with the Google Drive API.
    /// Acts as a Facade over the Google SDK, hiding pagination, retry logic and export handling.
    /// </summary>
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Retrieves all files from the authenticated user's Google Drive.
        /// Excludes folders, trashed files and files not owned by the user.
        /// Handles pagination automatically.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of all Drive files owned by the user.</returns>
        Task<IList<DriveFileInfo>> ListAllFilesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file from Google Drive as a stream.
        /// Google Workspace files (Docs, Sheets, Slides) are automatically exported
        /// to their Office equivalents (.docx, .xlsx, .pptx).
        /// </summary>
        /// <param name="file">The Drive file to download.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A stream containing the file content.</returns>
        Task<Stream> DownloadFileAsync(DriveFileInfo file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for files on Google Drive by name.
        /// Queries the entire Drive including shared files.
        /// </summary>
        /// <param name="query">The search term to match against file names.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of files matching the search query.</returns>
        Task<IList<DriveFileInfo>> SearchFilesAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Traverses or creates a nested folder path on Google Drive.
        /// For each segment in the path, checks if the folder exists and creates it if not.
        /// </summary>
        /// <param name="drivePath">The folder path (e.g. "Work/Reports/2024").</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The Google Drive folder ID of the final folder in the path.</returns>
        Task<string> GetOrCreateFolderPathAsync(string drivePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a local file to a specific Google Drive folder using resumable chunked upload.
        /// </summary>
        /// <param name="localPath">The full local file system path to the file.</param>
        /// <param name="driveFolderId">The Google Drive folder ID to upload into.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The Google Drive file ID of the uploaded file.</returns>
        /// <exception cref="Exception">Thrown when the upload fails.</exception>
        Task<string> UploadFileAsync(string localPath, string driveFolderId, CancellationToken cancellationToken = default);
    }
}