namespace GoogleDriveCLIManager.Services
{
    using Google.Apis.Drive.v3;
    using GoogleDriveCLIManager.Helpers;
    using GoogleDriveCLIManager.Models;
    using GoogleDriveCLIManager.Services.Interfaces;
    using File = Google.Apis.Drive.v3.Data.File;

    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService(DriveService driveService)
        {
            _driveService = driveService;
        }

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

        private static DriveFileInfo MapToDriveFileInfo(File file) => new()
        {
            Id = file.Id,
            Name = file.Name,
            MimeType = file.MimeType,
            Size = file.Size,
            ModifiedTime = file.ModifiedTimeDateTimeOffset?.UtcDateTime,
            ParentId = file.Parents?.FirstOrDefault()
        };

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

        private static string EscapeQuery(string query)
            => query.Replace("'", "\\'");

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