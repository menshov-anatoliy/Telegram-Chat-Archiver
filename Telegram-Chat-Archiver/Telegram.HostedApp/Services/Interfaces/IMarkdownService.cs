using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services.Interfaces;

/// <summary>
/// Интерфейс сервиса для работы с Markdown файлами
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Сохранить сообщения в Markdown файл
    /// </summary>
    /// <param name="messages">Сообщения для сохранения</param>
    /// <param name="chatTitle">Название чата</param>
    /// <param name="date">Дата для группировки сообщений</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task SaveMessagesAsync(IEnumerable<ChatMessage> messages, string chatTitle, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить сообщение в существующий Markdown файл
    /// </summary>
    /// <param name="message">Сообщение для добавления</param>
    /// <param name="chatTitle">Название чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task AppendMessageAsync(ChatMessage message, string chatTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить путь к Markdown файлу для указанной даты
    /// </summary>
    /// <param name="chatTitle">Название чата</param>
    /// <param name="date">Дата</param>
    /// <returns>Путь к файлу</returns>
    string GetMarkdownFilePath(string chatTitle, DateTime date);
}