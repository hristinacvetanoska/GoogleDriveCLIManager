namespace GoogleDriveCLIManager.Services.Interfaces
{
    /// <summary>
    /// Defines operations for local file system interactions.
    /// Abstracts file I/O to keep commands decoupled from the file system.
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Saves a stream as a file in the local Downloads directory.
        /// Creates any necessary subdirectories automatically.
        /// </summary>
        /// <param name="relativePath">The relative file path within the Downloads directory.</param>
        /// <param name="content">The stream containing the file content.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task SaveFileAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a file exists in the local Downloads directory.
        /// </summary>
        /// <param name="relativePath">The relative file path within the Downloads directory.</param>
        /// <returns>True if the file exists; otherwise false.</returns>
        bool FileExists(string relativePath);

        /// <summary>
        /// Returns the absolute path to the local Downloads directory.
        /// </summary>
        string GetDownloadsPath();

        /// <summary>
        /// Sanitizes a file name by replacing invalid characters with underscores
        /// and trimming leading/trailing dots and spaces.
        /// </summary>
        /// <param name="fileName">The raw file name to sanitize.</param>
        /// <returns>A file-system safe file name.</returns>
        string SanitizeFileName(string fileName);

        /// <summary>
        /// Creates a directory if it does not already exist.
        /// </summary>
        /// <param name="path">The full path of the directory to create.</param>
        void EnsureDirectoryExists(string path);
    }
}