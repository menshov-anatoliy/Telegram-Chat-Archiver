namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса для отправки уведомлений в Telegram
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    /// Отправить уведомление об ошибке в канал администратора
    /// </summary>
    /// <param name="error">Сообщение об ошибке</param>
    /// <param name="exception">Исключение, если есть</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SendErrorNotificationAsync(string error, Exception? exception = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправить информационное уведомление
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SendInfoNotificationAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить доступность канала для уведомлений
    /// </summary>
    /// <returns>True, если канал доступен</returns>
    Task<bool> IsNotificationChannelAvailableAsync();
}