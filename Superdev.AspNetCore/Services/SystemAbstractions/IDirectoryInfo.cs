namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public interface IDirectoryInfo
    {
        string Name { get; }

        string FullName { get; }

        bool Exists { get; }

        IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption);
    }
}