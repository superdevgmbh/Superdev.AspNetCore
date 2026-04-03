namespace Superdev.AspNetCore.Services
{
    [DebuggerDisplay("FileInfo: {this.Name}")]
    public class SystemFileInfo : IFileInfo
    {
        private readonly FileInfo fileInfo;

        public SystemFileInfo(FileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
        }

        public SystemFileInfo(string filePath) : this(new FileInfo(filePath))
        {
        }

        public string Name => this.fileInfo.Name;

        public string FullName => this.fileInfo.FullName;

        public bool Exists => this.fileInfo.Exists;

        public long Length => this.fileInfo.Length;
    }
}