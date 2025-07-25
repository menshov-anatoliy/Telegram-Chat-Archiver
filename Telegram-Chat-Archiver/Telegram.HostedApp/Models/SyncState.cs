namespace Telegram.HostedApp.Models;

/// <summary>
/// Состояние синхронизации чата
/// </summary>
public class SyncState
{
	/// <summary>
	/// ID чата
	/// </summary>
	public long ChatId { get; set; }

	/// <summary>
	/// ID последнего обработанного сообщения
	/// </summary>
	public int LastProcessedMessageId { get; set; }

	/// <summary>
	/// Дата последней синхронизации
	/// </summary>
	public DateTime LastSyncDate { get; set; }

	/// <summary>
	/// Общее количество обработанных сообщений
	/// </summary>
	public long TotalProcessedMessages { get; set; }

	/// <summary>
	/// Дата полной синхронизации
	/// </summary>
	public DateTime? LastFullSyncDate { get; set; }

	/// <summary>
	/// Состояние синхронизации
	/// </summary>
	public SyncStatus Status { get; set; }

	/// <summary>
	/// Сообщение об ошибке, если есть
	/// </summary>
	public string? ErrorMessage { get; set; }
}

/// <summary>
/// Статус синхронизации
/// </summary>
public enum SyncStatus
{
	/// <summary>
	/// Ожидание
	/// </summary>
	Idle,

	/// <summary>
	/// Синхронизация в процессе
	/// </summary>
	InProgress,

	/// <summary>
	/// Ошибка синхронизации
	/// </summary>
	Error,

	/// <summary>
	/// Приостановлено
	/// </summary>
	Paused
}

/// <summary>
/// Статистика обработки сообщений
/// </summary>
public class ProcessingStatistics
{
	/// <summary>
	/// Дата начала сессии
	/// </summary>
	public DateTime SessionStart { get; set; }

	/// <summary>
	/// Дата последнего обновления статистики
	/// </summary>
	public DateTime LastUpdate { get; set; }

	/// <summary>
	/// Общее количество обработанных сообщений
	/// </summary>
	public long TotalMessagesProcessed { get; set; }

	/// <summary>
	/// Количество скачанных медиафайлов
	/// </summary>
	public long MediaFilesDownloaded { get; set; }

	/// <summary>
	/// Общий размер скачанных файлов в байтах
	/// </summary>
	public long TotalDownloadedSizeBytes { get; set; }

	/// <summary>
	/// Количество ошибок
	/// </summary>
	public long ErrorCount { get; set; }

	/// <summary>
	/// Среднее время обработки сообщения в миллисекундах
	/// </summary>
	public double AverageProcessingTimeMs { get; set; }

	/// <summary>
	/// Статистика по типам сообщений
	/// </summary>
	public Dictionary<MessageType, long> MessageTypeStats { get; set; } = new();

	/// <summary>
	/// Статистика по авторам сообщений
	/// </summary>
	public Dictionary<long, AuthorStats> AuthorStats { get; set; } = new();

	/// <summary>
	/// Время последней архивации
	/// </summary>
	public DateTime? LastArchiveTime { get; set; }

	/// <summary>
	/// Количество созданных архивных файлов
	/// </summary>
	public long ArchiveFilesCreated { get; set; }
}

/// <summary>
/// Статистика по автору сообщений
/// </summary>
public class AuthorStats
{
	/// <summary>
	/// ID автора
	/// </summary>
	public long AuthorId { get; set; }

	/// <summary>
	/// Имя автора
	/// </summary>
	public string? AuthorName { get; set; }

	/// <summary>
	/// Количество сообщений
	/// </summary>
	public long MessageCount { get; set; }

	/// <summary>
	/// Количество медиафайлов
	/// </summary>
	public long MediaCount { get; set; }

	/// <summary>
	/// Дата первого сообщения
	/// </summary>
	public DateTime FirstMessageDate { get; set; }

	/// <summary>
	/// Дата последнего сообщения
	/// </summary>
	public DateTime LastMessageDate { get; set; }
}