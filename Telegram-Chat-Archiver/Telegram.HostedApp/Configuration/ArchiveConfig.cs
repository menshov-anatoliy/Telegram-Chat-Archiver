namespace Telegram.HostedApp.Configuration;

/// <summary>
/// Конфигурация архивирования сообщений
/// </summary>
public class ArchiveConfig
{
    /// <summary>
    /// Путь к папке для сохранения архивов
    /// </summary>
    public string OutputPath { get; set; } = "archives";

    /// <summary>
    /// Путь к папке для сохранения медиафайлов
    /// </summary>
    public string MediaPath { get; set; } = "media";

    /// <summary>
    /// Формат имени файла архива (теперь Markdown)
    /// </summary>
    public string FileNameFormat { get; set; } = "{Date:yyyy-MM-dd}.md";

    /// <summary>
    /// Интервал архивирования в минутах
    /// </summary>
    public int ArchiveIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Максимальное количество сообщений в одном файле
    /// </summary>
    public int MaxMessagesPerFile { get; set; } = 1000;

    /// <summary>
    /// Имя или ID чата для архивирования
    /// </summary>
    public string? TargetChat { get; set; }

    /// <summary>
    /// ID канала для отправки уведомлений об ошибках
    /// </summary>
    public string? ErrorNotificationChat { get; set; }
}