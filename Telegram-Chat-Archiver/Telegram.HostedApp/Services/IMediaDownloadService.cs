using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса для загрузки медиафайлов из Telegram
/// </summary>
public interface IMediaDownloadService
{
    /// <summary>
    /// Загрузить медиафайл и сохранить локально
    /// </summary>
    /// <param name="media">Информация о медиафайле</param>
    /// <param name="chatTitle">Название чата</param>
    /// <param name="messageDate">Дата сообщения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Обновленная информация о медиафайле с локальным путем</returns>
    Task<MediaInfo> DownloadMediaAsync(MediaInfo media, string chatTitle, DateTime messageDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить путь для сохранения медиафайла
    /// </summary>
    /// <param name="chatTitle">Название чата</param>
    /// <param name="messageDate">Дата сообщения</param>
    /// <param name="fileName">Имя файла</param>
    /// <returns>Путь для сохранения</returns>
    string GetMediaPath(string chatTitle, DateTime messageDate, string fileName);
}