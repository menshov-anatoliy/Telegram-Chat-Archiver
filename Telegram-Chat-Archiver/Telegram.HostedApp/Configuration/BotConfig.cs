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
}