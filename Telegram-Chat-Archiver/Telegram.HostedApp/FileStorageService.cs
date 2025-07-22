using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Telegram.HostedApp
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _workingDirectory;
        private readonly string _attachmentsFolder;
        private readonly string _fileNameMask;

        public FileStorageService(IConfiguration configuration)
        {
            _workingDirectory = configuration["Storage:WorkingDirectory"];
            _attachmentsFolder = configuration["Storage:AttachmentsFolder"];
            _fileNameMask = configuration["Formatting:FileNameMask"];
        }

        public Task<string> GetTodaysNotesFilePathAsync()
        {
            var fileName = DateTime.Now.ToString(_fileNameMask);
            var filePath = Path.Combine(_workingDirectory, fileName);
            return Task.FromResult(filePath);
        }

        public async Task AppendTextToNotesAsync(string text)
        {
            var filePath = await GetTodaysNotesFilePathAsync();
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await File.AppendAllTextAsync(filePath, text + Environment.NewLine + Environment.NewLine);
        }

        public async Task<string> SaveAttachmentAsync(byte[] content, string fileName)
        {
            var attachmentsPath = Path.Combine(_workingDirectory, _attachmentsFolder);
            Directory.CreateDirectory(attachmentsPath);
            var filePath = Path.Combine(attachmentsPath, fileName);
            await File.WriteAllBytesAsync(filePath, content);
            return filePath;
        }
    }
}
