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
    /// Формат имени файла архива
    /// </summary>
    public string FileNameFormat { get; set; } = "{ChatTitle}_{Date:yyyy-MM-dd}_{Time:HH-mm-ss}.json";

    /// <summary>
    /// Интервал архивирования в минутах
    /// </summary>
    public int ArchiveIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Максимальное количество сообщений в одном файле
    /// </summary>
    public int MaxMessagesPerFile { get; set; } = 1000;
}