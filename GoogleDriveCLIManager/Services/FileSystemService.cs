namespace GoogleDriveCLIManager.Services
{
    using GoogleDriveCLIManager.Services.Interfaces;

    public class FileSystemService : IFileSystemService
    {
        private readonly string _downloadsPath;

        public FileSystemService()
        {
            _downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            EnsureDirectoryExists(_downloadsPath);
        }

        public string GetDownloadsPath() => _downloadsPath;

        public bool FileExists(string relativePath)
        {
            var fullPath = Path.Combine(_downloadsPath, relativePath);
            return File.Exists(fullPath);
        }

        public async Task SaveFileAsync(string relativePath, Stream content,
            CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_downloadsPath, relativePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (directory is not null)
                EnsureDirectoryExists(directory);

            await using var fileStream = new FileStream(
                fullPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,       
                useAsync: true          
            );

            await content.CopyToAsync(fileStream, cancellationToken);
        }

        public string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Concat(
                fileName.Select(c => invalidChars.Contains(c) ? '_' : c)
            );

            return sanitized.Trim('.', ' ');
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}