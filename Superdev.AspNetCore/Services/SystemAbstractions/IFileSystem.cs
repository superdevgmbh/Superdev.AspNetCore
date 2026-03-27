namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public interface IFileSystem
    {
        string ReadAsString(string filePath);

        Task<string> ReadAsStringAsync(string filePath);

        Task<IFileInfo> CreateFileAsync(Stream stream, string filePath);

        Stream OpenRead(string filePath);

        Task<string> ReadAllTextAsync(string path);

        Task<IFileInfo> WriteAllTextAsync(string path, string contents);

        Task<IFileInfo> WriteAllBytesAsync(string path, byte[] bytes);

        IDirectoryInfo CreateDirectory(string path);

        bool FileExists(string path);

        void Move(string sourceFileName, string destFileName);

        DateTime GetLastWriteTime(string path);

        void Delete(string path);

        string GenerateTemporaryFilePath(string folderName, string fileName, Randomness randomness = Randomness.None);
    }

    public enum Randomness
    {
        None,
        Guid,
        DateTime
    }
}