using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Фоновый сервис для архивирования Telegram сообщений
/// </summary>
public class TelegramArchiverService : BackgroundService
{
    private readonly ILogger<TelegramArchiverService> _logger;
    private readonly TelegramConfig _telegramConfig;
    private readonly ArchiveConfig _archiveConfig;
    private readonly ITelegramArchiverService _archiverService;

    public TelegramArchiverService(
        ILogger<TelegramArchiverService> logger,
        IOptions<TelegramConfig> telegramConfig,
        IOptions<ArchiveConfig> archiveConfig,
        ITelegramArchiverService archiverService)
    {
        _logger = logger;
        _telegramConfig = telegramConfig.Value;
        _archiveConfig = archiveConfig.Value;
        _archiverService = archiverService;
    }

    /// <summary>
    /// Запуск фонового сервиса
    /// </summary>
    /// <param name="stoppingToken">Токен отмены</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telegram Archiver Service запущен");

        try
        {
            // Проверка конфигурации
            if (!ValidateConfiguration())
            {
                _logger.LogError("Конфигурация недействительна. Сервис будет остановлен.");
                return;
            }

            // Проверка подключения к Telegram
            var isConnected = await _archiverService.IsConnectedAsync();
            if (!isConnected)
            {
                _logger.LogError("Не удалось подключиться к Telegram API. Проверьте конфигурацию.");
                return;
            }

            _logger.LogInformation("Подключение к Telegram API успешно установлено");

            // Основной цикл архивирования
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformArchivingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка во время архивирования");
                }

                // Ожидание до следующего цикла
                var delay = TimeSpan.FromMinutes(_archiveConfig.ArchiveIntervalMinutes);
                _logger.LogInformation("Следующий цикл архивирования через {Delay} минут", _archiveConfig.ArchiveIntervalMinutes);
                
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Telegram Archiver Service остановлен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка в Telegram Archiver Service");
            throw;
        }
    }

    /// <summary>
    /// Выполнение архивирования
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    private async Task PerformArchivingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Начинается цикл архивирования");

        var chats = await _archiverService.GetChatsAsync();
        _logger.LogInformation("Найдено {ChatCount} чатов для архивирования", chats.Count());

        foreach (var chat in chats)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                _logger.LogInformation("Архивирование чата: {Chat}", chat);
                // Пока что используем заглушку для chatId
                await _archiverService.ArchiveChatAsync(0, cancellationToken);
                _logger.LogInformation("Архивирование чата {Chat} завершено", chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при архивировании чата {Chat}", chat);
            }
        }

        _logger.LogInformation("Цикл архивирования завершен");
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