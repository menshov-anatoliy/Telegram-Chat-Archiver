using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.HostedApp.Configuration;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для работы с Telegram Bot API
/// </summary>
public class TelegramBotService : ITelegramBotService, IDisposable
{
    private readonly ILogger<TelegramBotService> _logger;
    private readonly BotConfig _config;
    private readonly ITelegramArchiverService _archiverService;
    private readonly TelegramBotClient? _botClient;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;
    private bool _isListening;

    public TelegramBotService(
        ILogger<TelegramBotService> logger,
        IOptions<BotConfig> config,
        ITelegramArchiverService archiverService)
    {
        _logger = logger;
        _config = config.Value;
        _archiverService = archiverService;
        _cancellationTokenSource = new CancellationTokenSource();

        if (!string.IsNullOrEmpty(_config.BotToken))
        {
            _botClient = new TelegramBotClient(_config.BotToken);
        }
        else
        {
            _logger.LogWarning("Bot token не настроен, Bot API недоступен");
        }
    }

    /// <summary>
    /// Отправить сообщение администратору
    /// </summary>
    public async Task SendAdminMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_botClient == null || _config.AdminUserId == 0 || !_config.EnableBotNotifications)
        {
            _logger.LogDebug("Bot не настроен или отключен, пропускаем отправку сообщения: {Message}", message);
            return;
        }

        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: _config.AdminUserId,
                text: TruncateMessage(message),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Сообщение отправлено администратору: {Message}", message);
            
            // Задержка между сообщениями
            if (_config.MessageDelayMs > 0)
            {
                await Task.Delay(_config.MessageDelayMs, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения администратору");
        }
    }

    /// <summary>
    /// Отправить уведомление об ошибке
    /// </summary>
    public async Task SendErrorNotificationAsync(string error, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("🚨 <b>Ошибка в Telegram Chat Archiver</b>");
        errorMessage.AppendLine();
        errorMessage.AppendLine($"<b>Сообщение:</b> {error}");
        
        if (exception != null)
        {
            errorMessage.AppendLine($"<b>Тип:</b> {exception.GetType().Name}");
            errorMessage.AppendLine($"<b>Детали:</b> <code>{exception.Message}</code>");
        }
        
        errorMessage.AppendLine($"<b>Время:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        await SendAdminMessageAsync(errorMessage.ToString(), cancellationToken);
    }

    /// <summary>
    /// Отправить отчет о статистике
    /// </summary>
    public async Task SendStatisticsReportAsync(object statistics, CancellationToken cancellationToken = default)
    {
        if (statistics == null) return;

        var report = new StringBuilder();
        report.AppendLine("📊 <b>Отчет о статистике архивирования</b>");
        report.AppendLine();

        // Здесь будет форматирование различных типов статистики
        // Пока что просто выводим ToString()
        report.AppendLine($"<code>{statistics}</code>");
        
        report.AppendLine($"<b>Время отчета:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        await SendAdminMessageAsync(report.ToString(), cancellationToken);
    }

    /// <summary>
    /// Обработать команду управления
    /// </summary>
    public async Task<string> ProcessManagementCommandAsync(string command, long userId, CancellationToken cancellationToken = default)
    {
        if (userId != _config.AdminUserId)
        {
            return "❌ Доступ запрещен. Только администратор может использовать команды управления.";
        }

        return command.ToLower() switch
        {
            "/start" => "👋 Добро пожаловать! Telegram Chat Archiver активен.",
            "/status" => await GetStatusAsync(),
            "/stats" => await GetStatisticsAsync(),
            "/help" => GetHelpMessage(),
            _ => "❓ Неизвестная команда. Используйте /help для списка доступных команд."
        };
    }

    /// <summary>
    /// Проверить доступность бота
    /// </summary>
    public async Task<bool> IsBotAvailableAsync()
    {
        if (_botClient == null) return false;

        try
        {
            var me = await _botClient.GetMeAsync();
            _logger.LogDebug("Bot доступен: @{Username}", me.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке доступности бота");
            return false;
        }
    }

    /// <summary>
    /// Запустить прослушивание команд
    /// </summary>
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (_botClient == null || !_config.EnableManagementCommands || _isListening)
        {
            return;
        }

        _logger.LogInformation("Запуск прослушивания команд бота");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // получать все типы обновлений
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );

        _isListening = true;

        // Отправим уведомление о запуске
        await SendAdminMessageAsync("🚀 Telegram Chat Archiver запущен и готов к управлению", cancellationToken);
    }

    /// <summary>
    /// Остановить прослушивание команд
    /// </summary>
    public async Task StopListeningAsync()
    {
        if (!_isListening) return;

        _logger.LogInformation("Остановка прослушивания команд бота");
        _cancellationTokenSource.Cancel();
        _isListening = false;

        await SendAdminMessageAsync("⏹️ Telegram Chat Archiver остановлен");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is not { } messageText)
            return;

        _logger.LogDebug("Получена команда: {Text} от пользователя {UserId}", messageText, message.From?.Id);

        try
        {
            var response = await ProcessManagementCommandAsync(messageText, message.From?.Id ?? 0, cancellationToken);
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: response,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке команды");
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Произошла ошибка при обработке команды",
                cancellationToken: cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, "Ошибка в Bot API: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    private async Task<string> GetStatusAsync()
    {
        var status = new StringBuilder();
        status.AppendLine("🔄 <b>Статус системы</b>");
        
        var isConnected = await _archiverService.IsConnectedAsync();
        status.AppendLine($"Подключение к Telegram: {(isConnected ? "✅ Активно" : "❌ Отсутствует")}");
        
        status.AppendLine($"Время работы: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        return status.ToString();
    }

    private async Task<string> GetStatisticsAsync()
    {
        // Здесь будет получение реальной статистики из сервисов
        await Task.Delay(100); // заглушка
        
        return "📊 <b>Статистика</b>\n\nСтатистика временно недоступна.";
    }

    private static string GetHelpMessage()
    {
        return """
               📋 <b>Доступные команды:</b>
               
               /start - Приветствие
               /status - Статус системы
               /stats - Статистика обработки
               /help - Это сообщение
               """;
    }

    private string TruncateMessage(string message)
    {
        if (message.Length <= _config.MaxMessageLength)
            return message;

        return message[..(_config.MaxMessageLength - 3)] + "...";
    }

    public void Dispose()
    {
        if (_disposed) return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}