namespace Superdev.AspNetCore.Services
{
    [DebuggerDisplay("DirectoryInfo: {this.Name}")]
    public class SystemDirectoryInfo : IDirectoryInfo
    {
        private readonly DirectoryInfo directoryInfo;

        public SystemDirectoryInfo(DirectoryInfo directoryInfo)
        {
            this.directoryInfo = directoryInfo;
        }

        public string Name => this.directoryInfo.Name;

        public string FullName => this.directoryInfo.FullName;

        public bool Exists => this.directoryInfo.Exists;

        public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            return this.directoryInfo.GetFiles(searchPattern, searchOption).Select(f => new SystemFileInfo(f)).ToArray();
        }
    }
}