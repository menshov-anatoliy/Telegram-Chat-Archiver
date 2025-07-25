using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Сервис для работы в режиме реального времени с использованием Polling
/// Упрощенная реализация, заменяющая периодическое архивирование
/// </summary>
public class TelegramPollingService : BackgroundService
{
	private readonly ILogger<TelegramPollingService> _logger;
	private readonly TelegramConfig _telegramConfig;
	private readonly ArchiveConfig _archiveConfig;
	private readonly MarkdownArchiver _markdownArchiver;
	private readonly IMediaDownloadService _mediaDownloadService;
	private readonly ITelegramNotificationService _notificationService;
	private readonly ISyncStateService _syncStateService;
	private readonly IStatisticsService _statisticsService;
	private readonly ITelegramArchiverService _archiverService;

	public TelegramPollingService(
		ILogger<TelegramPollingService> logger,
		IOptions<TelegramConfig> telegramConfig,
		IOptions<ArchiveConfig> archiveConfig,
		MarkdownArchiver markdownArchiver,
		IMediaDownloadService mediaDownloadService,
		ITelegramNotificationService notificationService,
		ISyncStateService syncStateService,
		IStatisticsService statisticsService,
		ITelegramArchiverService archiverService)
	{
		_logger = logger;
		_telegramConfig = telegramConfig.Value;
		_archiveConfig = archiveConfig.Value;
		_markdownArchiver = markdownArchiver;
		_mediaDownloadService = mediaDownloadService;
		_notificationService = notificationService;
		_syncStateService = syncStateService;
		_statisticsService = statisticsService;
		_archiverService = archiverService;
	}

	/// <summary>
	/// Запуск polling сервиса
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Запуск Telegram Polling Service для работы в режиме реального времени");

		// Небольшая задержка чтобы дать веб-серверу время запуститься
		await Task.Delay(5000, stoppingToken);

		try
		{
			// Проверка конфигурации
			if (!ValidateConfiguration())
			{
				_logger.LogError("Конфигурация Telegram недействительна. Telegram функции будут отключены.");
				await NotifyConfigurationError(stoppingToken);
				return;
			}

			// Проверка подключения к Telegram (с retry)
			var isConnected = await TryConnectToTelegramAsync(stoppingToken);
			if (!isConnected)
			{
				_logger.LogError("Не удалось подключиться к Telegram API. Telegram функции будут отключены.");
				await NotifyConnectionError(stoppingToken);
				return;
			}

			_logger.LogInformation("Подключение к Telegram API успешно установлено");
			await _notificationService.SendInfoNotificationAsync(
				"Telegram Chat Archiver переключен в режим реального времени (инкрементальное архивирование)", stoppingToken);

			// Загрузка статистики
			await _statisticsService.LoadStatisticsAsync();

			// Основной цикл с уменьшенным интервалом для более частой проверки новых сообщений
			await RunPollingLoop(stoppingToken);
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Telegram Polling Service остановлен");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Критическая ошибка в Telegram Polling Service");
			await _notificationService.SendErrorNotificationAsync("Критическая ошибка в Polling Service", ex, stoppingToken);
		}
	}

	/// <summary>
	/// Основной цикл polling с инкрементальным архивированием
	/// </summary>
	private async Task RunPollingLoop(CancellationToken stoppingToken)
	{
		var lastReportTime = DateTime.UtcNow;
		// Уменьшенный интервал для более частой проверки (каждые 30 секунд вместо минут)
		var pollingInterval = TimeSpan.FromSeconds(30);

		_logger.LogInformation("Запуск цикла инкрементального архивирования (интервал: {Interval} сек)", pollingInterval.TotalSeconds);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var archiveStartTime = DateTime.UtcNow;
				
				// Выполнение инкрементального архивирования
				await PerformIncrementalArchivingAsync(stoppingToken);

				// Сохранение статистики
				await _statisticsService.SaveStatisticsAsync();

				// Периодические отчеты
				if (DateTime.UtcNow - lastReportTime > TimeSpan.FromMinutes(_archiveConfig.ReportIntervalMinutes))
				{
					await SendPeriodicReportAsync();
					lastReportTime = DateTime.UtcNow;
				}

				var archiveEndTime = DateTime.UtcNow;
				var archiveDuration = archiveEndTime - archiveStartTime;
				
				_logger.LogDebug("Цикл инкрементального архивирования завершен за {Duration:mm\\:ss}", archiveDuration);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка во время инкрементального архивирования");
				await _statisticsService.RecordErrorAsync("Ошибка в цикле инкрементального архивирования", ex);
				await _notificationService.SendErrorNotificationAsync("Ошибка в цикле архивирования", ex, stoppingToken);
			}

			// Ожидание до следующего цикла
			_logger.LogDebug("Следующий цикл архивирования через {Delay} секунд", pollingInterval.TotalSeconds);
			
			await Task.Delay(pollingInterval, stoppingToken);
		}
	}

	/// <summary>
	/// Выполнение инкрементального архивирования
	/// Получает только новые сообщения и добавляет их инкрементально
	/// </summary>
	private async Task PerformIncrementalArchivingAsync(CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(_archiveConfig.TargetChat))
		{
			_logger.LogDebug("Целевой чат не настроен. Пропускаем архивирование.");
			return;
		}

		try
		{
			// Ищем целевой чат
			var targetChat = await _archiverService.FindChatAsync(_archiveConfig.TargetChat);
			if (!targetChat.HasValue)
			{
				_logger.LogWarning("Целевой чат '{TargetChat}' не найден", _archiveConfig.TargetChat);
				return;
			}

			var (chatId, chatTitle) = targetChat.Value;
			_logger.LogDebug("Проверка новых сообщений в чате: {ChatTitle} (ID: {ChatId})", chatTitle, chatId);

			// Установка статуса синхронизации
			await _syncStateService.SetSyncStatusAsync(chatId, SyncStatus.InProgress);

			// Получаем небольшое количество последних сообщений (только новые)
			// Используем малый лимит для инкрементального подхода
			var messages = await _archiverService.GetMessagesAsync(chatId, limit: 20, cancellationToken: cancellationToken);
			var messagesList = messages.ToList();
			
			if (!messagesList.Any())
			{
				_logger.LogDebug("Нет новых сообщений в чате {ChatTitle}", chatTitle);
				await _syncStateService.SetSyncStatusAsync(chatId, SyncStatus.Idle);
				return;
			}

			_logger.LogDebug("Найдено {MessageCount} сообщений для инкрементальной обработки", messagesList.Count);

			// Обрабатываем сообщения по одному (инкрементально)
			foreach (var message in messagesList.OrderBy(m => m.Date))
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				try
				{
					// Загружаем медиафайлы если есть
					if (message.Media != null)
					{
						try
						{
							message.Media = await _mediaDownloadService.DownloadMediaAsync(
								message.Media, chatTitle, message.Date, cancellationToken);
						}
						catch (Exception ex)
						{
							_logger.LogWarning(ex, "Не удалось загрузить медиафайл для сообщения {MessageId}", message.Id);
						}
					}

					// Инкрементально добавляем сообщение в архив
					await _markdownArchiver.AppendMessageAsync(message, chatTitle, cancellationToken);

					// Записываем статистику
					await _statisticsService.RecordMessageProcessedAsync(message, 0.1); // Малое время обработки для инкрементального режима

					_logger.LogDebug("Сообщение {MessageId} успешно добавлено в архив", message.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Ошибка при обработке сообщения {MessageId}", message.Id);
					await _statisticsService.RecordErrorAsync($"Ошибка обработки сообщения {message.Id}", ex);
				}
			}

			// Запись статистики о создании архива
			await _statisticsService.RecordArchiveCreatedAsync();
			
			_logger.LogDebug("Инкрементальное архивирование чата {ChatTitle} завершено. Обработано {MessageCount} сообщений", 
				chatTitle, messagesList.Count);

			// Установка статуса завершения
			await _syncStateService.SetSyncStatusAsync(chatId, SyncStatus.Idle);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при инкрементальном архивировании целевого чата {TargetChat}", _archiveConfig.TargetChat);
			
			// Установка статуса ошибки
			if (!string.IsNullOrEmpty(_archiveConfig.TargetChat))
			{
				var chatInfo = await _archiverService.FindChatAsync(_archiveConfig.TargetChat);
				if (chatInfo.HasValue)
				{
					await _syncStateService.SetSyncStatusAsync(chatInfo.Value.ChatId, SyncStatus.Error, ex.Message);
				}
			}
			
			throw;
		}
	}

	/// <summary>
	/// Попытка подключения к Telegram с повторами
	/// </summary>
	private async Task<bool> TryConnectToTelegramAsync(CancellationToken cancellationToken)
	{
		const int maxRetries = 3;
		const int delayBetweenRetriesMs = 5000;

		for (int i = 1; i <= maxRetries; i++)
		{
			try
			{
				_logger.LogInformation("Попытка подключения к Telegram API #{Attempt}", i);
				var isConnected = await _archiverService.IsConnectedAsync();
				if (isConnected)
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Попытка подключения #{Attempt} не удалась", i);
			}

			if (i < maxRetries && !cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(delayBetweenRetriesMs, cancellationToken);
			}
		}

		return false;
	}

	/// <summary>
	/// Отправка периодического отчета
	/// </summary>
	private async Task SendPeriodicReportAsync()
	{
		try
		{
			var statistics = await _statisticsService.GetStatisticsAsync();
			_logger.LogInformation("Периодический отчет (инкрементальный режим): {Statistics}", statistics);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при отправке периодического отчета");
		}
	}

	/// <summary>
	/// Валидация конфигурации
	/// </summary>
	private bool ValidateConfiguration()
	{
		if (_telegramConfig.ApiId <= 0)
		{
			_logger.LogError("ApiId не настроен или имеет некорректное значение");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_telegramConfig.ApiHash))
		{
			_logger.LogError("ApiHash не настроен");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_telegramConfig.PhoneNumber))
		{
			_logger.LogError("PhoneNumber не настроен");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_archiveConfig.OutputPath))
		{
			_logger.LogError("OutputPath не настроен");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Уведомление об ошибке конфигурации
	/// </summary>
	private async Task NotifyConfigurationError(CancellationToken cancellationToken)
	{
		try
		{
			await _notificationService.SendErrorNotificationAsync("Ошибка конфигурации Telegram", cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Не удалось отправить уведомление об ошибке конфигурации");
		}
	}

	/// <summary>
	/// Уведомление об ошибке подключения
	/// </summary>
	private async Task NotifyConnectionError(CancellationToken cancellationToken)
	{
		try
		{
			await _notificationService.SendErrorNotificationAsync("Не удалось подключиться к Telegram API", cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Не удалось отправить уведомление об ошибке подключения");
		}
	}
}