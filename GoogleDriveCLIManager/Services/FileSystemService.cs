namespace GoogleDriveCLIManager.Services
{
    using GoogleDriveCLIManager.Services.Interfaces;

    /// <summary>
    /// Handles all local file system operations for the application.
    /// Manages the Downloads directory, file saving, sanitization and directory creation.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        private readonly string _downloadsPath;

        /// <summary>
        /// Initializes a new instance of <see cref="FileSystemService"/>.
        /// Sets the Downloads directory path relative to the project root
        /// and ensures the directory exists.
        /// </summary>
        public FileSystemService()
        {
            _downloadsPath = Path.Combine(
                Path.GetDirectoryName(AppContext.BaseDirectory)!,
                "..", "..", "..",
                "Downloads"
                );
            _downloadsPath = Path.GetFullPath(_downloadsPath);
            EnsureDirectoryExists(_downloadsPath);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FileSystemService"/> with a custom path.
        /// Used for unit testing to inject a temporary directory.
        /// </summary>
        /// <param name="downloadsPath">The custom downloads directory path.</param>
        public FileSystemService(string downloadsPath)
        {
            _downloadsPath = downloadsPath;
            EnsureDirectoryExists(_downloadsPath);
        }

        /// <summary>
        /// Returns the absolute path to the local Downloads directory.
        /// </summary>
        public string GetDownloadsPath() => _downloadsPath;

        /// <summary>
        /// Checks whether a file exists in the local Downloads directory.
        /// </summary>
        /// <param name="relativePath">The relative file path within the Downloads directory.</param>
        /// <returns>True if the file exists; otherwise false.</returns>
        public bool FileExists(string relativePath)
        {
            var fullPath = Path.Combine(_downloadsPath, relativePath);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Saves a stream as a file in the local Downloads directory.
        /// Uses async I/O with an 80KB buffer for optimal performance during parallel downloads.
        /// Creates any necessary subdirectories automatically.
        /// </summary>
        /// <param name="relativePath">The relative file path within the Downloads directory.</param>
        /// <param name="content">The stream containing the file content to save.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
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

        /// <summary>
        /// Sanitizes a file name by replacing invalid file system characters with underscores
        /// and trimming leading/trailing dots and spaces.
        /// Ensures the file name is valid across Windows, Mac and Linux.
        /// </summary>
        /// <param name="fileName">The raw file name to sanitize.</param>
        /// <returns>
        /// A file-system safe file name.
        /// Example: "report: final/2024.pdf" → "report_ final_2024.pdf"
        /// </returns>
        public string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Concat(
                fileName.Select(c => invalidChars.Contains(c) ? '_' : c)
            );

            return sanitized.Trim('.', ' ');
        }

        /// <summary>
        /// Creates a directory at the specified path if it does not already exist.
        /// </summary>
        /// <param name="path">The full path of the directory to create.</param>
        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}