namespace Telegram.HostedApp.Configuration;

/// <summary>
/// Конфигурация для Telegram Bot API
/// </summary>
public class BotConfig
{
	/// <summary>
	/// Токен Telegram Bot API
	/// </summary>
	public string BotToken { get; set; } = string.Empty;

	/// <summary>
	/// ID администратора для получения уведомлений
	/// </summary>
	public long AdminUserId { get; set; }

	/// <summary>
	/// Включить Bot API для уведомлений
	/// </summary>
	public bool EnableBotNotifications { get; set; } = true;

	/// <summary>
	/// Включить команды управления
	/// </summary>
	public bool EnableManagementCommands { get; set; } = true;

	/// <summary>
	/// Задержка между отправками сообщений в миллисекундах
	/// </summary>
	public int MessageDelayMs { get; set; } = 1000;

	/// <summary>
	/// Максимальная длина сообщения для отправки
	/// </summary>
	public int MaxMessageLength { get; set; } = 4096;

	/// <summary>
	/// Принудительно очищать pending updates при запуске
	/// </summary>
	public bool ClearPendingUpdatesOnStart { get; set; } = true;

	/// <summary>
	/// Максимальное количество попыток переподключения при ошибках polling
	/// </summary>
	public int MaxPollingRetries { get; set; } = 3;

	/// <summary>
	/// Задержка между попытками переподключения в миллисекундах
	/// </summary>
	public int PollingRetryDelayMs { get; set; } = 5000;

	/// <summary>
	/// Автоматически перезапускать polling при конфликтах
	/// </summary>
	public bool AutoRestartOnConflict { get; set; } = true;

	/// <summary>
	/// Интервал автоматического перезапуска в секундах
	/// </summary>
	public int AutoRestartDelaySeconds { get; set; } = 10;

	/// <summary>
	/// Отключить Bot API полностью при повторных конфликтах
	/// </summary>
	public bool DisableOnRepeatedConflicts { get; set; } = false;

	/// <summary>
	/// Максимальное количество конфликтов перед отключением
	/// </summary>
	public int MaxConflictsBeforeDisable { get; set; } = 5;
}