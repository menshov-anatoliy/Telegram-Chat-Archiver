using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Whisper.net;
using Whisper.net.Ggml;

namespace Telegram.HostedApp
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly WhisperFactory _whisperFactory;
        private readonly string _modelName;

        public TranscriptionService(IConfiguration configuration)
        {
            _modelName = configuration["Whisper:ModelName"];
            _whisperFactory = WhisperFactory.FromPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _modelName));
        }

        public async Task<string> TranscribeAsync(Stream audioStream)
        {
            using var processor = _whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            var result = await processor.ProcessAsync(audioStream);
            return result;
        }
    }
}
