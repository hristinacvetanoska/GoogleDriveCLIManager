namespace GoogleDriveCLIManager.Infrastructure.Exceptions
{
    public class DriveAuthenticationException : Exception
    {
        public DriveAuthenticationException(string message, Exception? inner = null)
            : base(message, inner) { }
    }

    public class DriveDownloadException : Exception
    {
        public string FileId { get; }
        public string FileName { get; }

        public DriveDownloadException(string fileId, string fileName, string message, Exception? inner = null)
            : base(message, inner)
        {
            FileId = fileId;
            FileName = fileName;
        }
    }

    public class DriveUploadException : Exception
    {
        public DriveUploadException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}