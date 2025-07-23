namespace Telegram.HostedApp.Configuration;

/// <summary>
/// Конфигурация для подключения к Telegram API
/// </summary>
public class TelegramConfig
{
    /// <summary>
    /// API ID приложения из my.telegram.org
    /// </summary>
    public int ApiId { get; set; }

    /// <summary>
    /// API Hash приложения из my.telegram.org
    /// </summary>
    public string ApiHash { get; set; } = string.Empty;

    /// <summary>
    /// Номер телефона для авторизации
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Путь к файлу сессии
    /// </summary>
    public string SessionFile { get; set; } = "session.dat";
}