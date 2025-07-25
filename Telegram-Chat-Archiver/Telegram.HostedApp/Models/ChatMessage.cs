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
	/// Форматированный текст с сохранением разметки
	/// </summary>
	public string? FormattedText { get; set; }

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

	/// <summary>
	/// Содержимое сообщения, на которое отвечает данное
	/// </summary>
	public string? ReplyToMessageText { get; set; }

	/// <summary>
	/// Информация о пересланном сообщении
	/// </summary>
	public ForwardInfo? ForwardInfo { get; set; }

	/// <summary>
	/// Список медиафайлов в альбоме
	/// </summary>
	public List<MediaInfo>? MediaGroup { get; set; }

	/// <summary>
	/// Информация об опросе
	/// </summary>
	public PollInfo? Poll { get; set; }

	/// <summary>
	/// Автоматически сгенерированные теги
	/// </summary>
	public List<string>? Tags { get; set; }

	/// <summary>
	/// Хэш содержимого сообщения для дедупликации
	/// </summary>
	public string? ContentHash { get; set; }

	/// <summary>
	/// Обработано ли сообщение
	/// </summary>
	public bool IsProcessed { get; set; }

	/// <summary>
	/// Редактировалось ли сообщение
	/// </summary>
	public bool IsEdited { get; set; }

	/// <summary>
	/// Дата последнего редактирования
	/// </summary>
	public DateTime? EditDate { get; set; }
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
	/// Альбом медиафайлов
	/// </summary>
	MediaGroup,

	/// <summary>
	/// Опрос
	/// </summary>
	Poll,

	/// <summary>
	/// Голосование
	/// </summary>
	Quiz,
	
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

	/// <summary>
	/// Хэш файла для дедупликации
	/// </summary>
	public string? FileHash { get; set; }

	/// <summary>
	/// Загружен ли файл локально
	/// </summary>
	public bool IsDownloaded { get; set; }

	/// <summary>
	/// ID медиагруппы для альбомов
	/// </summary>
	public string? MediaGroupId { get; set; }
}

/// <summary>
/// Информация о пересланном сообщении
/// </summary>
public class ForwardInfo
{
	/// <summary>
	/// Имя отправителя оригинального сообщения
	/// </summary>
	public string? FromName { get; set; }

	/// <summary>
	/// ID отправителя оригинального сообщения
	/// </summary>
	public long? FromId { get; set; }

	/// <summary>
	/// Название канала или чата, откуда пересланно
	/// </summary>
	public string? FromChat { get; set; }

	/// <summary>
	/// Дата оригинального сообщения
	/// </summary>
	public DateTime? OriginalDate { get; set; }

	/// <summary>
	/// ID оригинального сообщения
	/// </summary>
	public int? OriginalMessageId { get; set; }
}

/// <summary>
/// Информация об опросе
/// </summary>
public class PollInfo
{
	/// <summary>
	/// Вопрос опроса
	/// </summary>
	public string Question { get; set; } = string.Empty;

	/// <summary>
	/// Варианты ответов
	/// </summary>
	public List<PollOption> Options { get; set; } = new();

	/// <summary>
	/// Общее количество голосов
	/// </summary>
	public int TotalVoterCount { get; set; }

	/// <summary>
	/// Является ли опрос анонимным
	/// </summary>
	public bool IsAnonymous { get; set; }

	/// <summary>
	/// Закрыт ли опрос
	/// </summary>
	public bool IsClosed { get; set; }

	/// <summary>
	/// Является ли опрос викториной
	/// </summary>
	public bool IsQuiz { get; set; }
}

/// <summary>
/// Вариант ответа в опросе
/// </summary>
public class PollOption
{
	/// <summary>
	/// Текст варианта ответа
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Количество голосов за этот вариант
	/// </summary>
	public int VoterCount { get; set; }
}