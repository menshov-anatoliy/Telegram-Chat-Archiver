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
	private readonly TelegramBotClient? _botClient;
	private readonly CancellationTokenSource _cancellationTokenSource;
	private bool _disposed;
	private bool _isListening;
	private bool _isShuttingDown;
	private ISystemStatusProvider? _statusProvider;
	private int _pollingRetryCount;
	private static readonly SemaphoreSlim _instanceLock = new(1, 1);
	private static volatile bool _globalPollingActive = false;

	public TelegramBotService(
		ILogger<TelegramBotService> logger,
		IOptions<BotConfig> config)
	{
		_logger = logger;
		_config = config.Value;
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
	/// Установить провайдер статуса системы
	/// </summary>
	public void SetStatusProvider(ISystemStatusProvider statusProvider)
	{
		_statusProvider = statusProvider;
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
			_logger.LogDebug("Попытка отправки сообщения администратору (ID: {AdminUserId}): {Message}", 
				_config.AdminUserId, message);

			await _botClient.SendTextMessageAsync(
				chatId: _config.AdminUserId,
				text: TruncateMessage(message),
				parseMode: ParseMode.Html,
				cancellationToken: cancellationToken);

			_logger.LogDebug("Сообщение успешно отправлено администратору");
			
			// Задержка между сообщениями
			if (_config.MessageDelayMs > 0)
			{
				await Task.Delay(_config.MessageDelayMs, cancellationToken);
			}
		}
		catch (ApiRequestException apiEx) when (apiEx.Message.Contains("chat not found"))
		{
			_logger.LogWarning("Чат администратора не найден (ID: {AdminUserId}). " +
				"Администратор должен сначала написать боту команду /start. " +
				"Сообщение: {Message}", _config.AdminUserId, message);
			
			// Не бросаем исключение, чтобы не остановить работу приложения
		}
		catch (ApiRequestException apiEx) when (apiEx.Message.Contains("bot was blocked"))
		{
			_logger.LogWarning("Бот заблокирован администратором (ID: {AdminUserId}). " +
				"Сообщение: {Message}", _config.AdminUserId, message);
		}
		catch (ApiRequestException apiEx) when (apiEx.Message.Contains("user not found"))
		{
			_logger.LogWarning("Пользователь администратора не найден (ID: {AdminUserId}). " +
				"Проверьте правильность AdminUserId в конфигурации. " +
				"Сообщение: {Message}", _config.AdminUserId, message);
		}
		catch (ApiRequestException apiEx)
		{
			_logger.LogError(apiEx, "Ошибка Telegram API при отправке сообщения администратору (ID: {AdminUserId}): {ApiError}. " +
				"Сообщение: {Message}", _config.AdminUserId, apiEx.Message, message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Общая ошибка при отправке сообщения администратору (ID: {AdminUserId}). " +
				"Сообщение: {Message}", _config.AdminUserId, message);
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
		// Специальная обработка команды /start для любого пользователя
		if (command.ToLower() == "/start")
		{
			if (userId == _config.AdminUserId)
			{
				_logger.LogInformation("Администратор (ID: {AdminUserId}) инициализировал диалог с ботом", userId);
				return "👋 Добро пожаловать, администратор! Telegram Chat Archiver активен и готов к отправке уведомлений.";
			}
			else
			{
				_logger.LogInformation("Пользователь (ID: {UserId}) попытался использовать бота", userId);
				return $"👋 Добро пожаловать! Ваш Telegram ID: <code>{userId}</code>\n\n" +
					   "Этот бот предназначен для управления Telegram Chat Archiver. " +
					   "Доступ к командам управления имеет только администратор.\n\n" +
					   "Если вы администратор, укажите ваш ID в конфигурации приложения.";
			}
		}

		// Команда для получения своего ID (доступна всем)
		if (command.ToLower() == "/myid")
		{
			return $"🆔 Ваш Telegram ID: <code>{userId}</code>\n\n" +
				   "Скопируйте этот ID и укажите его в параметре AdminUserId в конфигурации бота.";
		}

		// Остальные команды только для администратора
		if (userId != _config.AdminUserId)
		{
			_logger.LogWarning("Пользователь (ID: {UserId}) попытался использовать команду администратора: {Command}", 
				userId, command);
			return "❌ Доступ запрещен. Только администратор может использовать команды управления.";
		}

		return command.ToLower() switch
		{
			"/status" => await GetStatusAsync(),
			"/stats" => await GetStatisticsAsync(),
			"/help" => GetHelpMessage(),
			"/restart_bot" => await RestartBotPollingAsync(),
			"/test_notifications" => await TestNotificationsAsync(),
			_ => "❓ Неизвестная команда. Используйте /help для списка доступных команд."
		};
	}

	/// <summary>
	/// Тестирование уведомлений
	/// </summary>
	private async Task<string> TestNotificationsAsync()
	{
		try
		{
			await SendAdminMessageAsync("🧪 Тест уведомлений: сообщение успешно отправлено!");
			return "✅ Тест уведомлений пройден успешно";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при тестировании уведомлений");
			return $"❌ Ошибка при тестировании уведомлений: {ex.Message}";
		}
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
			_logger.LogDebug("Bot доступен: @{Username} (ID: {BotId})", me.Username, me.Id);
			
			// Дополнительная проверка: можем ли мы отправить сообщение администратору
			if (_config.AdminUserId != 0 && _config.EnableBotNotifications)
			{
				await ValidateAdminAccessAsync();
			}
			
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при проверке доступности бота");
			return false;
		}
	}

	/// <summary>
	/// Проверка доступности администратора для отправки сообщений
	/// </summary>
	private async Task ValidateAdminAccessAsync()
	{
		try
		{
			// Пытаемся получить информацию о чате с администратором
			var chat = await _botClient!.GetChatAsync(_config.AdminUserId);
			_logger.LogInformation("Администратор найден: {FirstName} {LastName} (@{Username})", 
				chat.FirstName, chat.LastName, chat.Username);
		}
		catch (ApiRequestException apiEx) when (apiEx.Message.Contains("chat not found"))
		{
			_logger.LogWarning("⚠️ Администратор (ID: {AdminUserId}) не найден. " +
				"Администратор должен сначала написать боту команду /start для инициализации диалога.", 
				_config.AdminUserId);
		}
		catch (ApiRequestException apiEx) when (apiEx.Message.Contains("user not found"))
		{
			_logger.LogError("❌ Пользователь с ID {AdminUserId} не существует. " +
				"Проверьте правильность AdminUserId в конфигурации.", _config.AdminUserId);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Не удалось проверить доступность администратора (ID: {AdminUserId})", 
				_config.AdminUserId);
		}
	}

	/// <summary>
	/// Запустить прослушивание команд
	/// </summary>
	public async Task StartListeningAsync(CancellationToken cancellationToken = default)
	{
		if (_botClient == null || !_config.EnableManagementCommands || _isShuttingDown)
		{
			_logger.LogDebug("Bot не может быть запущен: BotClient={BotClientExists}, EnableCommands={EnableCommands}, ShuttingDown={ShuttingDown}", 
				_botClient != null, _config.EnableManagementCommands, _isShuttingDown);
			return;
		}

		await _instanceLock.WaitAsync(cancellationToken);
		try
		{
			if (_globalPollingActive)
			{
				_logger.LogWarning("Polling уже активен в другом экземпляре, пропускаем запуск");
				return;
			}

			if (_isListening)
			{
				_logger.LogDebug("Bot уже прослушивает команды");
				return;
			}

			await StartPollingWithRetryAsync(cancellationToken);
		}
		finally
		{
			_instanceLock.Release();
		}
	}

	/// <summary>
	/// Запуск polling с повторными попытками
	/// </summary>
	private async Task StartPollingWithRetryAsync(CancellationToken cancellationToken)
	{
		for (int attempt = 1; attempt <= _config.MaxPollingRetries; attempt++)
		{
			try
			{
				_logger.LogInformation("Попытка запуска прослушивания команд бота #{Attempt}", attempt);

				// Агрессивная очистка pending updates
				await ForceCleanupPendingUpdatesAsync();

				var receiverOptions = new ReceiverOptions
				{
					AllowedUpdates = new[] { UpdateType.Message }, // Только сообщения
					Limit = 1, // Ограничиваем количество обновлений за раз
					ThrowPendingUpdates = true // Игнорируем старые обновления
				};

				// Создаём новый токен для каждой попытки
				using var pollingCts = CancellationTokenSource.CreateLinkedTokenSource(
					_cancellationTokenSource.Token, cancellationToken);

				_botClient!.StartReceiving(
					updateHandler: HandleUpdateAsync,
					pollingErrorHandler: HandlePollingErrorAsync,
					receiverOptions: receiverOptions,
					cancellationToken: pollingCts.Token
				);

				_isListening = true;
				_globalPollingActive = true;
				_pollingRetryCount = 0;
				
				_logger.LogInformation("Прослушивание команд бота запущено успешно");

				// Не отправляем уведомление о запуске автоматически
				// Администратор должен сначала инициализировать диалог командой /start
				_logger.LogInformation("Bot готов к приему команд. Администратор может написать /start для инициализации диалога.");
				return; // Успешный запуск
			}
			catch (ApiRequestException apiEx) when (apiEx.Message.Contains("Conflict"))
			{
				_logger.LogWarning("Конфликт polling при попытке #{Attempt}: {Message}", attempt, apiEx.Message);
				
				if (attempt < _config.MaxPollingRetries)
				{
					var delay = _config.PollingRetryDelayMs * attempt; // Увеличиваем задержку
					_logger.LogInformation("Ожидание {Delay}ms перед следующей попыткой", delay);
					await Task.Delay(delay, cancellationToken);
					
					// Дополнительная очистка между попытками
					await ForceCleanupPendingUpdatesAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка при запуске прослушивания команд бота (попытка #{Attempt})", attempt);
				
				if (attempt < _config.MaxPollingRetries)
				{
					await Task.Delay(_config.PollingRetryDelayMs, cancellationToken);
				}
			}
		}

		_logger.LogError("Не удалось запустить прослушивание команд после {MaxRetries} попыток", _config.MaxPollingRetries);
	}

	/// <summary>
	/// Принудительная очистка pending updates
	/// </summary>
	private async Task ForceCleanupPendingUpdatesAsync()
	{
		try
		{
			if (_botClient == null) return;

			_logger.LogDebug("Принудительная очистка pending updates...");
			
			// Получаем максимальное количество обновлений
			var updates = await _botClient.GetUpdatesAsync(limit: 100, timeout: 1);
			
			if (updates.Length > 0)
			{
				_logger.LogInformation("Найдено {UpdateCount} pending updates, очищаем...", updates.Length);
				
				// Подтверждаем все обновления + сдвигаем offset
				var lastUpdateId = updates[updates.Length - 1].Id;
				await _botClient.GetUpdatesAsync(offset: lastUpdateId + 1, limit: 1, timeout: 1);
				
				// Дополнительная проверка - повторяем до полной очистки
				var remainingUpdates = await _botClient.GetUpdatesAsync(limit: 1, timeout: 1);
				if (remainingUpdates.Length > 0)
				{
					_logger.LogDebug("Повторная очистка оставшихся updates...");
					await _botClient.GetUpdatesAsync(offset: remainingUpdates[0].Id + 1, limit: 1, timeout: 1);
				}
				
				_logger.LogInformation("Pending updates очищены успешно");
			}
			else
			{
				_logger.LogDebug("Pending updates отсутствуют");
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Не удалось выполнить принудительную очистку pending updates");
		}
	}

	/// <summary>
	/// Остановить прослушивание команд
	/// </summary>
	public async Task StopListeningAsync()
	{
		if (!_isListening || _botClient == null) 
		{
			_logger.LogDebug("Bot уже остановлен или не инициализирован");
			return;
		}

		await _instanceLock.WaitAsync();
		try
		{
			_isShuttingDown = true;
			_logger.LogInformation("Остановка прослушивания команд бота");

			// Отменяем токен для остановки receiving
			_cancellationTokenSource.Cancel();

			// Ждем достаточно времени для корректной остановки
			await Task.Delay(3000);

			_isListening = false;
			_globalPollingActive = false;
			
			_logger.LogInformation("Прослушивание команд бота остановлено");

			// Отправляем уведомление об остановке (но не ждем, если есть проблемы)
			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				await SendAdminMessageAsync("⏹️ Telegram Chat Archiver остановлен", cts.Token);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Не удалось отправить уведомление об остановке");
			}
		}
		finally
		{
			_instanceLock.Release();
		}
	}

	/// <summary>
	/// Перезапуск polling бота
	/// </summary>
	private async Task<string> RestartBotPollingAsync()
	{
		try
		{
			_logger.LogInformation("Перезапуск polling бота по запросу администратора");
			
			await StopListeningAsync();
			await Task.Delay(2000); // Ждем полной остановки
			await StartListeningAsync();
			
			return "🔄 Polling бота перезапущен успешно";
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при перезапуске polling бота");
			return $"❌ Ошибка при перезапуске: {ex.Message}";
		}
	}

	private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		// Проверяем, не был ли отменен токен
		if (cancellationToken.IsCancellationRequested || _cancellationTokenSource.Token.IsCancellationRequested || _isShuttingDown)
		{
			return;
		}

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
		catch (OperationCanceledException)
		{
			_logger.LogDebug("Обработка команды отменена");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при обработке команды");
			
			try
			{
				await botClient.SendTextMessageAsync(
					chatId: message.Chat.Id,
					text: "❌ Произошла ошибка при обработке команды",
					cancellationToken: cancellationToken);
			}
			catch (Exception sendEx)
			{
				_logger.LogError(sendEx, "Не удалось отправить сообщение об ошибке");
			}
		}
	}

	private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		// Если токен отменен или происходит остановка, это нормально
		if (cancellationToken.IsCancellationRequested || _cancellationTokenSource.Token.IsCancellationRequested || _isShuttingDown)
		{
			_logger.LogDebug("Polling остановлен по токену отмены или shutdown");
			return Task.CompletedTask;
		}

		var errorMessage = exception switch
		{
			ApiRequestException apiRequestException => HandleApiRequestException(apiRequestException),
			_ => exception.ToString()
		};

		// Не логируем как ошибку, если это конфликт (который мы обрабатываем)
		if (exception is ApiRequestException apiEx && apiEx.Message.Contains("Conflict"))
		{
			_logger.LogWarning("Конфликт в Bot API (будет обработан): {ErrorMessage}", errorMessage);
		}
		else
		{
			_logger.LogError(exception, "Ошибка в Bot API: {ErrorMessage}", errorMessage);
		}
		
		return Task.CompletedTask;
	}

	private string HandleApiRequestException(ApiRequestException apiRequestException)
	{
		// Обработка конкретных ошибок API
		if (apiRequestException.Message.Contains("Conflict: terminated by other getUpdates request"))
		{
			_logger.LogWarning("Обнаружен конфликт getUpdates. Останавливаем текущий polling.");
			
			// Помечаем как неактивный и планируем перезапуск
			_isListening = false;
			_globalPollingActive = false;
			
			// Планируем автоматический перезапуск через некоторое время
			_ = Task.Run(async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(10));
				if (!_isShuttingDown && !_isListening)
				{
					_logger.LogInformation("Попытка автоматического перезапуска после конфликта");
					try
					{
						await StartListeningAsync();
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Не удалось автоматически перезапустить после конфликта");
					}
				}
			});
			
			return "Конфликт с другим экземпляром бота (автоматический перезапуск)";
		}

		return $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
	}

	private async Task<string> GetStatusAsync()
	{
		var status = new StringBuilder();
		status.AppendLine("🔄 <b>Статус системы</b>");
		
		if (_statusProvider != null)
		{
			var isConnected = await _statusProvider.IsTelegramConnectedAsync();
			status.AppendLine($"Подключение к Telegram: {(isConnected ? "✅ Активно" : "❌ Отсутствует")}");
		}
		else
		{
			status.AppendLine("Подключение к Telegram: ❓ Статус недоступен");
		}
		
		status.AppendLine($"Bot прослушивание: {(_isListening ? "✅ Активно" : "❌ Неактивно")}");
		status.AppendLine($"Глобальный polling: {(_globalPollingActive ? "✅ Активен" : "❌ Неактивен")}");
		status.AppendLine($"Попыток переподключения: {_pollingRetryCount}");
		status.AppendLine($"Время работы: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
		
		return status.ToString();
	}

	private async Task<string> GetStatisticsAsync()
	{
		if (_statusProvider != null)
		{
			return await _statusProvider.GetSystemStatisticsAsync();
		}
		
		return "📊 <b>Статистика</b>\n\nСтатистика временно недоступна.";
	}

	private static string GetHelpMessage()
	{
		return """
			   📋 <b>Доступные команды:</b>
			   
			   🔧 <b>Общие команды:</b>
			   /start - Инициализация диалога с ботом
			   /myid - Получить свой Telegram ID
			   
			   ⚙️ <b>Команды администратора:</b>
			   /status - Статус системы
			   /stats - Статистика обработки
			   /test_notifications - Тест отправки уведомлений
			   /restart_bot - Перезапуск Bot polling
			   /help - Это сообщение
			   
			   💡 <b>Примечание:</b> Если вы получаете ошибки "chat not found", 
			   обязательно выполните команду /start для инициализации диалога.
			   
			   🔑 <b>Для администраторов:</b> Используйте /myid для получения вашего ID
			   и укажите его в параметре AdminUserId в конфигурации.
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

		try
		{
			_isShuttingDown = true;
			
			// Сначала останавливаем прослушивание
			if (_isListening)
			{
				StopListeningAsync().GetAwaiter().GetResult();
			}

			// Затем отменяем токен и освобождаем ресурсы
			_cancellationTokenSource.Cancel();
			_cancellationTokenSource.Dispose();
			
			_globalPollingActive = false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при освобождении ресурсов TelegramBotService");
		}
		finally
		{
			_disposed = true;
		}
	}
}