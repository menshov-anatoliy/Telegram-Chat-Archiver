using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telegram.HostedApp
{
    public class ArchiverService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorageService;
        private readonly ITranscriptionService _transcriptionService;
        private CancellationTokenSource _cancellationTokenSource;

        public ArchiverService(ITelegramBotClient botClient, IConfiguration configuration, IFileStorageService fileStorageService, ITranscriptionService transcriptionService)
        {
            _botClient = botClient;
            _configuration = configuration;
            _fileStorageService = fileStorageService;
            _transcriptionService = transcriptionService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                _cancellationTokenSource.Token
            );

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            Console.WriteLine("ArchiverService stopped.");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process messages
            if (update.Message is not { } message)
                return;
            // Only process messages from the target chat
            var chatId = long.Parse(_configuration["Telegram:ChatId"]);
            if (message.Chat.Id != chatId)
                return;

            try
            {
                var headerMask = _configuration["Formatting:HeaderMask"];
                var header = DateTime.Now.ToString(headerMask);

                if (message.ForwardFrom != null || message.ForwardFromChat != null)
                {
                    header += " (forwarded)";
                }

                if (message.Text is { } text)
                {
                    var content = $"{header}\n{text}";
                    await _fileStorageService.AppendTextToNotesAsync(content);
                }
                else if (message.Photo is { } photo)
                {
                    var fileId = photo.Last().FileId;
                    var file = await _botClient.GetFileAsync(fileId, cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await _botClient.DownloadFileAsync(file.FilePath, memoryStream, cancellationToken);
                    var savedFilePath = await _fileStorageService.SaveAttachmentAsync(memoryStream.ToArray(), Path.GetFileName(file.FilePath));
                    var content = $"{header}\n![{Path.GetFileName(savedFilePath)}]({savedFilePath})";
                    await _fileStorageService.AppendTextToNotesAsync(content);
                }
                else if (message.Document is { } document)
                {
                    var file = await _botClient.GetFileAsync(document.FileId, cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await _botClient.DownloadFileAsync(file.FilePath, memoryStream, cancellationToken);
                    var savedFilePath = await _fileStorageService.SaveAttachmentAsync(memoryStream.ToArray(), document.FileName);
                    var content = $"{header}\n![{document.FileName}]({savedFilePath})";
                    await _fileStorageService.AppendTextToNotesAsync(content);
                }
                else if (message.Voice is { } voice)
                {
                    var file = await _botClient.GetFileAsync(voice.FileId, cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await _botClient.DownloadFileAsync(file.FilePath, memoryStream, cancellationToken);
                    memoryStream.Position = 0;
                    var transcription = await _transcriptionService.TranscribeAsync(memoryStream);
                    var content = $"{header}\n{transcription}";
                    await _fileStorageService.AppendTextToNotesAsync(content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, $"Error processing message: {ex.Message}", cancellationToken: cancellationToken);
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Polling error: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
