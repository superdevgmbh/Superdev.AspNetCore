using System.Text;

namespace Superdev.AspNetCore.Services.SystemAbstractions
{
    public class FileSystem : IFileSystem
    {
        public string ReadAsString(string filePath)
        {
            using (var streamReader = new StreamReader(filePath, Encoding.Default))
            {
                return streamReader.ReadToEnd();
            }
        }

        public async Task<IFileInfo> CreateFileAsync(Stream stream, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fs);
            }

            return new SystemFileInfo(filePath);
        }

        public Stream OpenRead(string filePath)
        {
            return File.OpenRead(filePath);
        }

        public async Task<string> ReadAsStringAsync(string filePath)
        {
            using (var streamReader = new StreamReader(filePath, Encoding.Default))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            return await File.ReadAllTextAsync(path, Encoding.Default);
        }

        public async Task<IFileInfo> WriteAllTextAsync(string path, string contents)
        {
            await File.WriteAllTextAsync(path, contents, Encoding.Default);

            return new SystemFileInfo(path);
        }

        public async Task<IFileInfo> WriteAllBytesAsync(string path, byte[] bytes)
        {
            await File.WriteAllBytesAsync(path, bytes);

            return new SystemFileInfo(path);
        }

        public IDirectoryInfo CreateDirectory(string path)
        {
            var exportDirectoryInfo = new DirectoryInfo(path);
            if (!exportDirectoryInfo.Exists)
            {
                exportDirectoryInfo.Create();
            }

            return new SystemDirectoryInfo(exportDirectoryInfo);
        }

        public void Move(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public string GenerateTemporaryFilePath(string folderName, string fileName, Randomness randomness = Randomness.None)
        {
            return Path.Combine(Path.GetTempPath(), folderName, $"{Path.GetFileNameWithoutExtension(fileName)}{GenerateRandomness(randomness)}{Path.GetExtension(fileName)}");
        }

        private static string? GenerateRandomness(Randomness randomness)
        {
            switch (randomness)
            {
                case Randomness.Guid:
                    return $"-{Guid.NewGuid()}";
                case Randomness.DateTime:
                    return $"-{DateTime.Now:yyyy-dd-M--HH-mm-ss}";
                default:
                    return null;
            }
        }
    }
}