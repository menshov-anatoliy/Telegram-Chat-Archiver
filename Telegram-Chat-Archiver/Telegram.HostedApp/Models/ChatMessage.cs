namespace Telegram.HostedApp.Models;

/// <summary>
/// Модель сообщения чата
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Дата и время сообщения
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Имя автора сообщения
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// ID автора сообщения
    /// </summary>
    public long AuthorId { get; set; }

    /// <summary>
    /// Текстовое содержимое сообщения
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Тип сообщения
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Информация о медиафайле, если есть
    /// </summary>
    public MediaInfo? Media { get; set; }

    /// <summary>
    /// ID сообщения, на которое отвечает данное сообщение
    /// </summary>
    public int? ReplyToMessageId { get; set; }
}

/// <summary>
/// Тип сообщения
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Текстовое сообщение
    /// </summary>
    Text,
    
    /// <summary>
    /// Изображение
    /// </summary>
    Photo,
    
    /// <summary>
    /// Документ
    /// </summary>
    Document,
    
    /// <summary>
    /// Голосовое сообщение
    /// </summary>
    Voice,
    
    /// <summary>
    /// Видео
    /// </summary>
    Video,
    
    /// <summary>
    /// Стикер
    /// </summary>
    Sticker,
    
    /// <summary>
    /// Неизвестный тип
    /// </summary>
    Unknown
}

/// <summary>
/// Информация о медиафайле
/// </summary>
public class MediaInfo
{
    /// <summary>
    /// Имя файла
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Размер файла в байтах
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// MIME тип файла
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Локальный путь к скачанному файлу
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Ширина изображения или видео
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Высота изображения или видео
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Длительность аудио или видео в секундах
    /// </summary>
    public int? Duration { get; set; }
}