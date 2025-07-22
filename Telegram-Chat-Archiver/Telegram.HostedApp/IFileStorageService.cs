using System.Threading.Tasks;

namespace Telegram.HostedApp
{
    public interface IFileStorageService
    {
        Task<string> GetTodaysNotesFilePathAsync();
        Task AppendTextToNotesAsync(string text);
        Task<string> SaveAttachmentAsync(byte[] content, string fileName);
    }
}
