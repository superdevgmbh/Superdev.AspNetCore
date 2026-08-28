namespace Superdev.AspNetCore.Services
{
    public interface IFileInfo
    {
        string Name { get; }

        string FullName { get; }

        bool Exists { get; }

        long Length { get; }
    }
}