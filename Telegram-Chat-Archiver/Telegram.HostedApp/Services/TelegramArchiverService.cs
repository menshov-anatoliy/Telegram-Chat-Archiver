using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Фоновый сервис для архивирования Telegram сообщений с расширенной функциональностью
/// </summary>
public class TelegramArchiverService : BackgroundService
{
    private readonly ILogger<TelegramArchiverService> _logger;
    private readonly TelegramConfig _telegramConfig;
    private readonly ArchiveConfig _archiveConfig;
    private readonly BotConfig _botConfig;
    private readonly ITelegramArchiverService _archiverService;
    private readonly ITelegramBotService _botService;
    private readonly ISyncStateService _syncStateService;
    private readonly IStatisticsService _statisticsService;
    private readonly ITelegramNotificationService _notificationService;

    public TelegramArchiverService(
        ILogger<TelegramArchiverService> logger,
        IOptions<TelegramConfig> telegramConfig,
        IOptions<ArchiveConfig> archiveConfig,
        IOptions<BotConfig> botConfig,
        ITelegramArchiverService archiverService,
        ITelegramBotService botService,
        ISyncStateService syncStateService,
        IStatisticsService statisticsService,
        ITelegramNotificationService notificationService)
    {
        _logger = logger;
        _telegramConfig = telegramConfig.Value;
        _archiveConfig = archiveConfig.Value;
        _botConfig = botConfig.Value;
        _archiverService = archiverService;
        _botService = botService;
        _syncStateService = syncStateService;
        _statisticsService = statisticsService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Запуск фонового сервиса
    /// </summary>
    /// <param name="stoppingToken">Токен отмены</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Archiver Service с расширенной функциональностью запущен");

        try
        {
            // Инициализация сервисов
            await InitializeServicesAsync(stoppingToken);

            // Проверка конфигурации
            if (!ValidateConfiguration())
            {
                _logger.LogError("Конфигурация недействительна. Сервис будет остановлен.");
                await _notificationService.SendErrorNotificationAsync("Ошибка конфигурации при запуске сервиса");
                return;
            }

            // Проверка подключения к Telegram
            var isConnected = await _archiverService.IsConnectedAsync();
            if (!isConnected)
            {
                _logger.LogError("Не удалось подключиться к Telegram API. Проверьте конфигурацию.");
                await _notificationService.SendErrorNotificationAsync("Не удалось подключиться к Telegram API");
                return;
            }

            _logger.LogInformation("Подключение к Telegram API успешно установлено");
            await _notificationService.SendInfoNotificationAsync("Telegram Chat Archiver успешно запущен и подключен к API");

            // Загрузка статистики
            await _statisticsService.LoadStatisticsAsync();

            // Основной цикл архивирования
            await RunMainArchivingLoop(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telegram Archiver Service остановлен");
            await _notificationService.SendInfoNotificationAsync("Telegram Chat Archiver остановлен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка в Telegram Archiver Service");
            await _notificationService.SendErrorNotificationAsync("Критическая ошибка в сервисе", ex);
        }
        finally
        {
            // Graceful shutdown
            await GracefulShutdownAsync();
        }
    }

    /// <summary>
    /// Инициализация сервисов
    /// </summary>
    private async Task InitializeServicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Инициализация сервисов...");

        // Запуск Bot API для команд управления
        if (_botConfig.EnableManagementCommands)
        {
            try
            {
                await _botService.StartListeningAsync(cancellationToken);
                _logger.LogInformation("Bot API запущен для команд управления");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось запустить Bot API для управления");
            }
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
                await _statisticsService.SaveStatisticsAsync();

                // Периодические отчеты
                if (DateTime.UtcNow - lastReportTime > TimeSpan.FromMinutes(_archiveConfig.ReportIntervalMinutes))
                {
                    await SendPeriodicReportAsync();
                    lastReportTime = DateTime.UtcNow;
                }

                var archiveEndTime = DateTime.UtcNow;
                var archiveDuration = archiveEndTime - archiveStartTime;
                
                _logger.LogInformation("Цикл архивирования завершен за {Duration:mm\\:ss}", archiveDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка во время архивирования");
                await _statisticsService.RecordErrorAsync("Ошибка в цикле архивирования", ex);
                await _notificationService.SendErrorNotificationAsync("Ошибка в цикле архивирования", ex);
            }

            // Ожидание до следующего цикла
            var delay = TimeSpan.FromMinutes(_archiveConfig.ArchiveIntervalMinutes);
            _logger.LogInformation("Следующий цикл архивирования через {Delay} минут", _archiveConfig.ArchiveIntervalMinutes);
            
            await Task.Delay(delay, stoppingToken);
        }
    }

    /// <summary>
    /// Выполнение архивирования
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task PerformArchivingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Начинается цикл архивирования");

        // Проверяем настройку целевого чата
        if (string.IsNullOrWhiteSpace(_archiveConfig.TargetChat))
        {
            _logger.LogWarning("Целевой чат не настроен. Пропускаем архивирование.");
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
            _logger.LogInformation("Найден целевой чат: {ChatTitle} (ID: {ChatId})", chatTitle, chatId);

            // Установка статуса синхронизации
            await _syncStateService.SetSyncStatusAsync(chatId, Models.SyncStatus.InProgress);

            // Архивируем целевой чат
            await _archiverService.ArchiveChatAsync(chatId, cancellationToken);
            
            // Запись статистики о создании архива
            await _statisticsService.RecordArchiveCreatedAsync();
            
            _logger.LogInformation("Архивирование чата {ChatTitle} завершено успешно", chatTitle);

            // Установка статуса завершения
            await _syncStateService.SetSyncStatusAsync(chatId, Models.SyncStatus.Idle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при архивировании целевого чата {TargetChat}", _archiveConfig.TargetChat);
            
            // Установка статуса ошибки
            if (!string.IsNullOrEmpty(_archiveConfig.TargetChat))
            {
                var chatInfo = await _archiverService.FindChatAsync(_archiveConfig.TargetChat);
                if (chatInfo.HasValue)
                {
                    await _syncStateService.SetSyncStatusAsync(chatInfo.Value.ChatId, Models.SyncStatus.Error, ex.Message);
                }
            }
            
            throw;
        }

        _logger.LogInformation("Цикл архивирования завершен");
    }

    /// <summary>
    /// Отправка периодического отчета
    /// </summary>
    private async Task SendPeriodicReportAsync()
    {
        try
        {
            var statistics = await _statisticsService.GetStatisticsAsync();
            await _botService.SendStatisticsReportAsync(statistics);
            
            _logger.LogInformation("Периодический отчет отправлен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке периодического отчета");
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
    /// Graceful shutdown
    /// </summary>
    private async Task GracefulShutdownAsync()
    {
        try
        {
            _logger.LogInformation("Выполнение graceful shutdown...");

            // Остановка Bot API
            await _botService.StopListeningAsync();

            // Сохранение финальной статистики
            await _statisticsService.SaveStatisticsAsync();

            _logger.LogInformation("Graceful shutdown завершен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при graceful shutdown");
        }
    }

    /// <summary>
    /// Остановка сервиса
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка Telegram Archiver Service...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Telegram Archiver Service остановлен");
    }
}