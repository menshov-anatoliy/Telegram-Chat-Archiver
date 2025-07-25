using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using Telegram.HostedApp.Services.Interfaces;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Фоновый сервис для архивирования Telegram сообщений с расширенной функциональностью
/// </summary>
public class TelegramArchiverBackgroundService(
	ILogger<TelegramArchiverBackgroundService> logger,
	IOptions<TelegramConfig> telegramConfig,
	IOptions<ArchiveConfig> archiveConfig,
	IOptions<BotConfig> botConfig,
	ITelegramArchiverService archiverService,
	ITelegramBotService botService,
	ISyncStateService syncStateService,
	IStatisticsService statisticsService,
	ITelegramNotificationService notificationService)
	: BackgroundService, ISystemStatusProvider
{
	private readonly TelegramConfig _telegramConfig = telegramConfig.Value;
	private readonly ArchiveConfig _archiveConfig = archiveConfig.Value;
	private readonly BotConfig _botConfig = botConfig.Value;

	/// <summary>
	/// Получить статус подключения к Telegram
	/// </summary>
	public async Task<bool> IsTelegramConnectedAsync()
	{
		try
		{
			return await archiverService.IsConnectedAsync();
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Получить статистику системы
	/// </summary>
	public async Task<string> GetSystemStatisticsAsync()
	{
		try
		{
			var statistics = await statisticsService.GetStatisticsAsync();
			return $"📊 <b>Статистика</b>\n\n<code>{statistics}</code>";
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при получении статистики");
			return "📊 <b>Статистика</b>\n\nОшибка при получении статистики.";
		}
	}

	/// <summary>
	/// Запуск фонового сервиса
	/// </summary>
	/// <param name="stoppingToken">Токен отмены</param>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Telegram Archiver Service с расширенной функциональностью запущен в фоновом режиме");

		// Небольшая задержка чтобы дать веб-серверу время запуститься
		await Task.Delay(5000, stoppingToken);

		try
		{
			// Инициализация сервисов
			await InitializeServicesAsync(stoppingToken);

			// Проверка конфигурации
			if (!ValidateConfiguration())
			{
				logger.LogError("Конфигурация Telegram недействительна. Telegram функции будут отключены.");
				await NotifyConfigurationError(stoppingToken);
				// Не останавливаем весь сервис, просто ждем
				await WaitIndefinitelyAsync(stoppingToken);
				return;
			}

			// Проверка подключения к Telegram (с retry)
			var isConnected = await TryConnectToTelegramAsync(stoppingToken);
			if (!isConnected)
			{
				logger.LogError("Не удалось подключиться к Telegram API. Telegram функции будут отключены.");
				await NotifyConnectionError(stoppingToken);
				// Не останавливаем весь сервис, просто ждем
				await WaitIndefinitelyAsync(stoppingToken);
				return;
			}

			logger.LogInformation("Подключение к Telegram API успешно установлено");
			await notificationService.SendInfoNotificationAsync("Telegram Chat Archiver успешно запущен и подключен к API", stoppingToken);

			// Загрузка статистики
			await statisticsService.LoadStatisticsAsync();

			// Основной цикл архивирования
			await RunMainArchivingLoop(stoppingToken);
		}
		catch (OperationCanceledException)
		{
			logger.LogInformation("Telegram Archiver Service остановлен");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка в Telegram Archiver Service, но веб-сервер продолжит работу");
			// Не бросаем исключение наверх, чтобы не останавливать весь хост
		}
		finally
		{
			// Graceful shutdown
			await GracefulShutdownAsync();
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
				logger.LogInformation("Попытка подключения к Telegram API #{Attempt}", i);
				var isConnected = await archiverService.IsConnectedAsync();
				if (isConnected)
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Попытка подключения #{Attempt} не удалась", i);
			}

			if (i < maxRetries && !cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(delayBetweenRetriesMs, cancellationToken);
			}
		}

		return false;
	}

	/// <summary>
	/// Ожидание без выполнения основной логики (когда Telegram недоступен)
	/// </summary>
	private async Task WaitIndefinitelyAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Переход в режим ожидания без активного архивирования");
		
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				logger.LogDebug("Сервис работает в режиме ожидания (Telegram недоступен)");
			}
		}
		catch (OperationCanceledException)
		{
			// Нормальная остановка
		}
	}

	/// <summary>
	/// Уведомление об ошибке конфигурации
	/// </summary>
	private async Task NotifyConfigurationError(CancellationToken cancellationToken)
	{
		try
		{
			await notificationService.SendErrorNotificationAsync("Ошибка конфигурации Telegram", cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Не удалось отправить уведомление об ошибке конфигурации");
		}
	}

	/// <summary>
	/// Уведомление об ошибке подключения
	/// </summary>
	private async Task NotifyConnectionError(CancellationToken cancellationToken)
	{
		try
		{
			await notificationService.SendErrorNotificationAsync("Не удалось подключиться к Telegram API", cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Не удалось отправить уведомление об ошибке подключения");
		}
	}

	/// <summary>
	/// Инициализация сервисов
	/// </summary>
	private async Task InitializeServicesAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Инициализация сервисов...");

		try
		{
			// Установим себя как провайдер статуса для bot service
			botService.SetStatusProvider(this);

			// Запуск Bot API для команд управления
			if (_botConfig.EnableManagementCommands)
			{
				try
				{
					await botService.StartListeningAsync(cancellationToken);
					logger.LogInformation("Bot API запущен для команд управления");
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Не удалось запустить Bot API для управления");
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при инициализации сервисов");
		}
	}

	/// <summary>
	/// Основной цикл архивирования
	/// </summary>
	private async Task RunMainArchivingLoop(CancellationToken stoppingToken)
	{
		var lastReportTime = DateTime.UtcNow;

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				var archiveStartTime = DateTime.UtcNow;
				
				// Выполнение архивирования
				await PerformArchivingAsync(stoppingToken);

				// Сохранение статистики
				await statisticsService.SaveStatisticsAsync();

				// Периодические отчеты
				if (DateTime.UtcNow - lastReportTime > TimeSpan.FromMinutes(_archiveConfig.ReportIntervalMinutes))
				{
					await SendPeriodicReportAsync();
					lastReportTime = DateTime.UtcNow;
				}

				var archiveEndTime = DateTime.UtcNow;
				var archiveDuration = archiveEndTime - archiveStartTime;
				
				logger.LogInformation("Цикл архивирования завершен за {Duration:mm\\:ss}", archiveDuration);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Ошибка во время архивирования");
				await statisticsService.RecordErrorAsync("Ошибка в цикле архивирования", ex);
				await notificationService.SendErrorNotificationAsync("Ошибка в цикле архивирования", ex, stoppingToken);
			}

			// Ожидание до следующего цикла
			var delay = TimeSpan.FromMinutes(_archiveConfig.ArchiveIntervalMinutes);
			logger.LogInformation("Следующий цикл архивирования через {Delay} минут", _archiveConfig.ArchiveIntervalMinutes);
			
			await Task.Delay(delay, stoppingToken);
		}
	}

	/// <summary>
	/// Выполнение архивирования
	/// </summary>
	/// <param name="cancellationToken">Токен отмены</param>
	private async Task PerformArchivingAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Начинается цикл архивирования");

		// Проверяем настройку целевого чата
		if (string.IsNullOrWhiteSpace(_archiveConfig.TargetChat))
		{
			logger.LogWarning("Целевой чат не настроен. Пропускаем архивирование.");
			return;
		}

		try
		{
			// Ищем целевой чат
			var targetChat = await archiverService.FindChatAsync(_archiveConfig.TargetChat);
			if (!targetChat.HasValue)
			{
				logger.LogWarning("Целевой чат '{TargetChat}' не найден", _archiveConfig.TargetChat);
				return;
			}

			var (chatId, chatTitle) = targetChat.Value;
			logger.LogInformation("Найден целевой чат: {ChatTitle} (ID: {ChatId})", chatTitle, chatId);

			// Установка статуса синхронизации
			await syncStateService.SetSyncStatusAsync(chatId, Models.SyncStatus.InProgress);

			// Архивируем целевой чат
			await archiverService.ArchiveChatAsync(chatId, cancellationToken);
			
			// Запись статистики о создании архива
			await statisticsService.RecordArchiveCreatedAsync();
			
			logger.LogInformation("Архивирование чата {ChatTitle} завершено успешно", chatTitle);

			// Установка статуса завершения
			await syncStateService.SetSyncStatusAsync(chatId, Models.SyncStatus.Idle);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при архивировании целевого чата {TargetChat}", _archiveConfig.TargetChat);
			
			// Установка статуса ошибки
			if (!string.IsNullOrEmpty(_archiveConfig.TargetChat))
			{
				var chatInfo = await archiverService.FindChatAsync(_archiveConfig.TargetChat);
				if (chatInfo.HasValue)
				{
					await syncStateService.SetSyncStatusAsync(chatInfo.Value.ChatId, Models.SyncStatus.Error, ex.Message);
				}
			}
			
			throw;
		}

		logger.LogInformation("Цикл архивирования завершен");
	}

	/// <summary>
	/// Отправка периодического отчета
	/// </summary>
	private async Task SendPeriodicReportAsync()
	{
		try
		{
			var statistics = await statisticsService.GetStatisticsAsync();
			await botService.SendStatisticsReportAsync(statistics);
			
			logger.LogInformation("Периодический отчет отправлен");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при отправке периодического отчета");
		}
	}

	/// <summary>
	/// Валидация конфигурации
	/// </summary>
	/// <returns>True, если конфигурация корректна</returns>
	private bool ValidateConfiguration()
	{
		if (_telegramConfig.ApiId <= 0)
		{
			logger.LogError("ApiId не настроен или имеет некорректное значение");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_telegramConfig.ApiHash))
		{
			logger.LogError("ApiHash не настроен");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_telegramConfig.PhoneNumber))
		{
			logger.LogError("PhoneNumber не настроен");
			return false;
		}

		if (string.IsNullOrWhiteSpace(_archiveConfig.OutputPath))
		{
			logger.LogError("OutputPath не настроен");
			return false;
		}

		return true;
	}

	/// <summary>
	/// Graceful shutdown
	/// </summary>
	private async Task GracefulShutdownAsync()
	{
		try
		{
			logger.LogInformation("Выполнение graceful shutdown...");

			// Остановка Bot API
			await botService.StopListeningAsync();

			// Сохранение финальной статистики
			await statisticsService.SaveStatisticsAsync();

			logger.LogInformation("Graceful shutdown завершен");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при graceful shutdown");
		}
	}

	/// <summary>
	/// Остановка сервиса
	/// </summary>
	/// <param name="cancellationToken">Токен отмены</param>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Остановка Telegram Archiver Service...");
		await base.StopAsync(cancellationToken);
		logger.LogInformation("Telegram Archiver Service остановлен");
	}
}