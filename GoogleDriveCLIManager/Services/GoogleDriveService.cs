namespace GoogleDriveCLIManager.Services
{
    using Google.Apis.Drive.v3;
    using GoogleDriveCLIManager.Helpers;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using File = Google.Apis.Drive.v3.Data.File;

    /// <summary>
    /// Implements <see cref="IGoogleDriveService"/> as a Facade over the Google Drive SDK.
    /// Centralizes all API interactions, pagination, retry logic and export handling.
    /// </summary>
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService(DriveService driveService)
        {
            _driveService = driveService;
        }

        /// <summary>
        /// Retrieves all files from the authenticated user's Google Drive.
        /// Excludes folders, trashed files and files not owned by the user.
        /// Handles pagination automatically.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of all Drive files owned by the user.</returns>
        public async Task<IList<DriveFileInfo>> ListAllFilesAsync(
            CancellationToken cancellationToken = default)
        {
            var result = new List<DriveFileInfo>();
            string? pageToken = null;

            do
            {
                var request = _driveService.Files.List();
                request.Fields = "nextPageToken, files(id, name, mimeType, size, modifiedTime, parents)";
                request.PageSize = 1000;
                request.PageToken = pageToken;
                request.Q = "trashed = false and 'me' in owners and mimeType != 'application/vnd.google-apps.folder'";

                var response = await ExecuteWithRetryAsync(
                    () => request.ExecuteAsync(cancellationToken));

                if (response.Files is not null)
                {
                    result.AddRange(response.Files.Select(MapToDriveFileInfo));
                }

                pageToken = response.NextPageToken;

            } while (pageToken is not null);

            return result;
        }

        /// <summary>
        /// Downloads a file from Google Drive as a stream.
        /// Google Workspace files (Docs, Sheets, Slides) are automatically exported
        /// to their Office equivalents (.docx, .xlsx, .pptx).
        /// </summary>
        /// <param name="file">The Drive file to download.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A stream containing the file content.</returns>
        public async Task<Stream> DownloadFileAsync(
            DriveFileInfo file, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            var exportMimeType = MimeTypeHelper.GetExportMimeType(file.MimeType);

            if (!string.IsNullOrEmpty(exportMimeType))
            {
                var exportRequest = _driveService.Files.Export(file.Id, exportMimeType);
                await exportRequest.DownloadAsync(memoryStream, cancellationToken);
            }
            else
            {
                var getRequest = _driveService.Files.Get(file.Id);
                await getRequest.DownloadAsync(memoryStream, cancellationToken);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Searches for files on Google Drive by name.
        /// Queries the entire Drive including shared files.
        /// </summary>
        /// <param name="query">The search term to match against file names.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A list of files matching the search query.</returns>
        public async Task<IList<DriveFileInfo>> SearchFilesAsync(
            string query, CancellationToken cancellationToken = default)
        {
            var result = new List<DriveFileInfo>();
            string? pageToken = null;

            do
            {
                var request = _driveService.Files.List();
                request.Fields = "nextPageToken, files(id, name, mimeType, size, modifiedTime, parents)";
                request.PageSize = 100;
                request.PageToken = pageToken;
                request.Q = $"name contains '{EscapeQuery(query)}' and trashed = false";

                var response = await ExecuteWithRetryAsync(
                    () => request.ExecuteAsync(cancellationToken));

                if (response.Files is not null)
                {
                    result.AddRange(response.Files.Select(MapToDriveFileInfo));
                }

                pageToken = response.NextPageToken;

            } while (pageToken is not null);

            return result;
        }

        /// <summary>
        /// Traverses or creates a nested folder path on Google Drive.
        /// For each segment in the path, checks if the folder exists and creates it if not.
        /// </summary>
        /// <param name="drivePath">The folder path (e.g. "Work/Reports/2024").</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The Google Drive folder ID of the final folder in the path.</returns>
        public async Task<string> GetOrCreateFolderPathAsync(
            string drivePath, CancellationToken cancellationToken = default)
        {
            if (drivePath.Equals("root", StringComparison.OrdinalIgnoreCase) ||
                drivePath.Equals("/", StringComparison.OrdinalIgnoreCase))
                return "root";

            var segments = drivePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var parentId = "root";

            foreach (var segment in segments)
            {
                var request = _driveService.Files.List();
                request.Q = $"name = '{EscapeQuery(segment)}' " +
                            $"and mimeType = 'application/vnd.google-apps.folder' " +
                            $"and '{parentId}' in parents " +
                            $"and trashed = false";
                request.Fields = "files(id, name)";

                var response = await ExecuteWithRetryAsync(
                    () => request.ExecuteAsync(cancellationToken));

                if (response.Files?.Count > 0)
                {
                    parentId = response.Files[0].Id;
                }
                else
                {
                    var folderMetadata = new File
                    {
                        Name = segment,
                        MimeType = "application/vnd.google-apps.folder",
                        Parents = new List<string> { parentId }
                    };

                    var createRequest = _driveService.Files.Create(folderMetadata);
                    createRequest.Fields = "id";
                    var folder = await ExecuteWithRetryAsync(
                        () => createRequest.ExecuteAsync(cancellationToken));

                    parentId = folder.Id;
                }
            }

            return parentId;
        }

        /// <summary>
        /// Uploads a local file to a specific Google Drive folder using resumable chunked upload.
        /// </summary>
        /// <param name="localPath">The full local file system path to the file.</param>
        /// <param name="driveFolderId">The Google Drive folder ID to upload into.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The Google Drive file ID of the uploaded file.</returns>
        /// <exception cref="Exception">Thrown when the upload fails.</exception>
        public async Task<string> UploadFileAsync(
            string localPath, string driveFolderId, CancellationToken cancellationToken = default)
        {
            var fileName = Path.GetFileName(localPath);
            var mimeType = GetMimeType(localPath);

            var fileMetadata = new File
            {
                Name = fileName,
                Parents = new List<string> { driveFolderId }
            };

            await using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read);

            var request = _driveService.Files.Create(fileMetadata, stream, mimeType);
            request.Fields = "id, name";
            request.ChunkSize = 256 * 1024;

            var response = await request.UploadAsync(cancellationToken);

            if (response.Status == Google.Apis.Upload.UploadStatus.Failed)
                throw new Exception($"Upload failed: {response.Exception?.Message}");

            return request.ResponseBody?.Id ?? string.Empty;
        }

        /// <summary>
        /// Maps a Google SDK File object to the application's DriveFileInfo domain model.
        /// </summary>
        /// <param name="file">The Google SDK file object.</param>
        /// <returns>A mapped <see cref="DriveFileInfo"/> instance.</returns>
        private static DriveFileInfo MapToDriveFileInfo(File file) => new()
        {
            Id = file.Id,
            Name = file.Name,
            MimeType = file.MimeType,
            Size = file.Size,
            ModifiedTime = file.ModifiedTimeDateTimeOffset?.UtcDateTime,
            ParentId = file.Parents?.FirstOrDefault()
        };

        /// <summary>
        /// Executes an API action with exponential backoff retry logic.
        /// Retries on rate limiting (HTTP 429) and service unavailability (HTTP 503).
        /// Delay doubles on each retry: 1s → 2s → 4s.
        /// </summary>
        /// <typeparam name="T">The return type of the API action.</typeparam>
        /// <param name="action">The async API action to execute.</param>
        /// <param name="maxRetries">Maximum number of retry attempts. Default is 3.</param>
        /// <returns>The result of the action.</returns>
        /// <exception cref="Google.GoogleApiException">Thrown when max retries are exceeded.</exception>
        private static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> action, int maxRetries = 3)
        {
            var delay = TimeSpan.FromSeconds(1);

            for (var attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await action();
                }
                catch (Google.GoogleApiException ex)
                    when (ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests
                          || ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    if (attempt == maxRetries) throw;

                    await Task.Delay(delay);
                    delay *= 2;
                }
            }

            throw new InvalidOperationException("Retry logic failed unexpectedly.");
        }

        /// <summary>
        /// Escapes single quotes in Drive API query strings to prevent query injection.
        /// </summary>
        /// <param name="query">The raw query string.</param>
        /// <returns>The escaped query string.</returns>
        private static string EscapeQuery(string query)
            => query.Replace("'", "\\'");


        /// <summary>
        /// Determines the MIME type of a local file based on its extension.
        /// Falls back to application/octet-stream for unknown types.
        /// </summary>
        /// <param name="filePath">The local file path.</param>
        /// <returns>The MIME type string.</returns>
        private static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".mp4" => "video/mp4",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}