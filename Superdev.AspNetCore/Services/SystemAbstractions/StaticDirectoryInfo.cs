namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public class StaticDirectoryInfo : IDirectoryInfo
    {
        private readonly IFileInfo[] files;

        public StaticDirectoryInfo(params IFileInfo[] files)
        {
            this.files = files;
        }

        public required string Name { get; set; }

        public required string FullName { get; set; }

        public bool Exists { get; set; }

        public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            return this.files;
        }
    }
}