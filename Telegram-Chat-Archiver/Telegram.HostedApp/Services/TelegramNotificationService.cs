using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Telegram.HostedApp.Configuration;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для отправки уведомлений в Telegram
/// </summary>
public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly ArchiveConfig _config;

    public TelegramNotificationService(ILogger<TelegramNotificationService> logger, IOptions<ArchiveConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Отправить уведомление об ошибке в канал администратора
    /// </summary>
    public async Task SendErrorNotificationAsync(string error, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ErrorNotificationChat))
            {
                _logger.LogWarning("Канал для уведомлений об ошибках не настроен");
                return;
            }

            var message = FormatErrorMessage(error, exception);
            
            // Пока что просто логируем сообщение
            // В реальной реализации здесь будет отправка через WTelegramClient
            _logger.LogWarning("Уведомление об ошибке (заглушка): {Message}", message);
            
            await Task.Delay(100, cancellationToken); // Имитация отправки
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления об ошибке");
        }
    }

    /// <summary>
    /// Отправить информационное уведомление
    /// </summary>
    public async Task SendInfoNotificationAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ErrorNotificationChat))
            {
                _logger.LogWarning("Канал для уведомлений не настроен");
                return;
            }

            var formattedMessage = FormatInfoMessage(message);
            
            // Пока что просто логируем сообщение
            // В реальной реализации здесь будет отправка через WTelegramClient
            _logger.LogInformation("Информационное уведомление (заглушка): {Message}", formattedMessage);
            
            await Task.Delay(100, cancellationToken); // Имитация отправки
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке информационного уведомления");
        }
    }

    /// <summary>
    /// Проверить доступность канала для уведомлений
    /// </summary>
    public async Task<bool> IsNotificationChannelAvailableAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ErrorNotificationChat))
            {
                _logger.LogDebug("Канал для уведомлений не настроен");
                return false;
            }

            // Пока что просто проверяем наличие конфигурации
            // В реальной реализации здесь будет проверка доступности канала через API
            await Task.Delay(50); // Имитация проверки
            
            _logger.LogDebug("Канал для уведомлений доступен (заглушка)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке доступности канала уведомлений");
            return false;
        }
    }

    /// <summary>
    /// Форматирование сообщения об ошибке
    /// </summary>
    private string FormatErrorMessage(string error, Exception? exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚨 **ОШИБКА В TELEGRAM ARCHIVER**");
        sb.AppendLine();
        sb.AppendLine($"**Время:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Ошибка:** {error}");
        
        if (exception != null)
        {
            sb.AppendLine();
            sb.AppendLine("**Детали исключения:**");
            sb.AppendLine($"```");
            sb.AppendLine($"{exception.GetType().Name}: {exception.Message}");
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                // Ограничиваем размер stack trace для Telegram
                var stackTrace = exception.StackTrace;
                if (stackTrace.Length > 2000)
                    stackTrace = stackTrace.Substring(0, 2000) + "...";
                sb.AppendLine(stackTrace);
            }
            sb.AppendLine($"```");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Форматирование информационного сообщения
    /// </summary>
    private string FormatInfoMessage(string message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ℹ️ **TELEGRAM ARCHIVER**");
        sb.AppendLine();
        sb.AppendLine($"**Время:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Сообщение:** {message}");
        
        return sb.ToString();
    }
}