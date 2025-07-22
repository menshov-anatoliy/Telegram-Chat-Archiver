using System.IO;
using System.Threading.Tasks;

namespace Telegram.HostedApp
{
    public interface ITranscriptionService
    {
        Task<string> TranscribeAsync(Stream audioStream);
    }
}
