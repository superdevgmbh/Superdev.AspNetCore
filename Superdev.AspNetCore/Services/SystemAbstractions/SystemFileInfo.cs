namespace Superdev.AspNetCore.Services
{
    public class StaticFileInfo : IFileInfo
    {
        public string Name { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public bool Exists { get; set; }

        public long Length { get; set; }
    }
}