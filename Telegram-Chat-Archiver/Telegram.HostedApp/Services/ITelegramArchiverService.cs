using Telegram.HostedApp.Models;

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
    /// Аутентификация пользователя в Telegram
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>True, если аутентификация успешна</returns>
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка доступных чатов
    /// </summary>
    /// <returns>Словарь чатов (ID -> название)</returns>
    Task<Dictionary<long, string>> GetChatsAsync();

    /// <summary>
    /// Поиск чата по имени или ID
    /// </summary>
    /// <param name="chatIdentifier">Имя чата или его ID</param>
    /// <returns>Информация о чате или null, если не найден</returns>
    Task<(long ChatId, string ChatTitle)?> FindChatAsync(string chatIdentifier);

    /// <summary>
    /// Получение сообщений из указанного чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="limit">Максимальное количество сообщений</param>
    /// <param name="offsetId">ID сообщения для смещения (для пагинации)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Список сообщений</returns>
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(long chatId, int limit = 100, int offsetId = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Архивирование сообщений из указанного чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ArchiveChatAsync(long chatId, CancellationToken cancellationToken = default);
}