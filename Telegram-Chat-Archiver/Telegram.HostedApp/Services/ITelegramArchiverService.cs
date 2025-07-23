namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса архивирования Telegram сообщений
/// </summary>
public interface ITelegramArchiverService
{
    /// <summary>
    /// Проверка подключения к Telegram API
    /// </summary>
    /// <returns>True, если подключение успешно</returns>
    Task<bool> IsConnectedAsync();

    /// <summary>
    /// Получение списка доступных чатов
    /// </summary>
    /// <returns>Список чатов</returns>
    Task<IEnumerable<string>> GetChatsAsync();

    /// <summary>
    /// Архивирование сообщений из указанного чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ArchiveChatAsync(long chatId, CancellationToken cancellationToken = default);
}