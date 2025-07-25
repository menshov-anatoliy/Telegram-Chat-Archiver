using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services.Interfaces;

/// <summary>
/// Интерфейс сервиса для сбора и управления статистикой
/// </summary>
public interface IStatisticsService : IDisposable
{
    /// <summary>
    /// Записать обработку сообщения
    /// </summary>
    /// <param name="message">Обработанное сообщение</param>
    /// <param name="processingTimeMs">Время обработки в миллисекундах</param>
    Task RecordMessageProcessedAsync(ChatMessage message, double processingTimeMs);

    /// <summary>
    /// Записать загрузку медиафайла
    /// </summary>
    /// <param name="fileSize">Размер файла в байтах</param>
    Task RecordMediaDownloadAsync(long fileSize);

    /// <summary>
    /// Записать ошибку
    /// </summary>
    /// <param name="error">Описание ошибки</param>
    /// <param name="exception">Исключение, если есть</param>
    Task RecordErrorAsync(string error, Exception? exception = null);

    /// <summary>
    /// Записать создание архивного файла
    /// </summary>
    Task RecordArchiveCreatedAsync();

    /// <summary>
    /// Получить текущую статистику
    /// </summary>
    /// <returns>Объект статистики</returns>
    Task<ProcessingStatistics> GetStatisticsAsync();

    /// <summary>
    /// Сбросить статистику сессии
    /// </summary>
    Task ResetSessionStatisticsAsync();

    /// <summary>
    /// Получить статистику по авторам
    /// </summary>
    /// <returns>Словарь статистики по авторам</returns>
    Task<Dictionary<long, AuthorStats>> GetAuthorStatisticsAsync();

    /// <summary>
    /// Получить статистику по типам сообщений
    /// </summary>
    /// <returns>Словарь статистики по типам сообщений</returns>
    Task<Dictionary<MessageType, long>> GetMessageTypeStatisticsAsync();

    /// <summary>
    /// Сохранить статистику в файл
    /// </summary>
    Task SaveStatisticsAsync();

    /// <summary>
    /// Загрузить статистику из файла
    /// </summary>
    Task LoadStatisticsAsync();
}