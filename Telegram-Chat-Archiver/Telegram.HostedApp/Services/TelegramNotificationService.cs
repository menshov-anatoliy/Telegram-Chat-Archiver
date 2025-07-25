using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services.Interfaces;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для отправки уведомлений в Telegram
/// </summary>
public class TelegramNotificationService : ITelegramNotificationService
{
	private readonly ILogger<TelegramNotificationService> _logger;
	private readonly ArchiveConfig _config;
	private readonly ITelegramBotService _botService;

	public TelegramNotificationService(
		ILogger<TelegramNotificationService> logger, 
		IOptions<ArchiveConfig> config,
		ITelegramBotService botService)
	{
		_logger = logger;
		_config = config.Value;
		_botService = botService;
	}

	/// <summary>
	/// Отправить уведомление об ошибке в канал администратора
	/// </summary>
	public async Task SendErrorNotificationAsync(string error, Exception? exception = null, CancellationToken cancellationToken = default)
	{
		try
		{
			// Отправляем через Bot API
			await _botService.SendErrorNotificationAsync(error, exception, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при отправке уведомления об ошибке");
			
			// Fallback: логируем сообщение
			var message = FormatErrorMessage(error, exception);
			_logger.LogWarning("Уведомление об ошибке (fallback logging): {Message}", message);
		}
	}

	/// <summary>
	/// Отправить информационное уведомление
	/// </summary>
	public async Task SendInfoNotificationAsync(string message, CancellationToken cancellationToken = default)
	{
		try
		{
			// Отправляем через Bot API
			await _botService.SendAdminMessageAsync(message, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при отправке информационного уведомления");
			
			// Fallback: логируем сообщение
			var formattedMessage = FormatInfoMessage(message);
			_logger.LogInformation("Информационное уведомление (fallback logging): {Message}", formattedMessage);
		}
	}

	/// <summary>
	/// Проверить доступность канала для уведомлений
	/// </summary>
	public async Task<bool> IsNotificationChannelAvailableAsync()
	{
		try
		{
			// Проверяем доступность Bot API
			return await _botService.IsBotAvailableAsync();
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