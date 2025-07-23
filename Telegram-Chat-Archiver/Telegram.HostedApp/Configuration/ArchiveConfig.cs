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
    /// Путь к папке для индексных файлов
    /// </summary>
    public string IndexPath { get; set; } = "indexes";

    /// <summary>
    /// Путь к файлу базы данных метаданных
    /// </summary>
    public string DatabasePath { get; set; } = "metadata.db";

    /// <summary>
    /// Путь к файлу состояния синхронизации
    /// </summary>
    public string SyncStatePath { get; set; } = "sync_state.json";

    /// <summary>
    /// Формат имени файла архива (теперь Markdown)
    /// </summary>
    public string FileNameFormat { get; set; } = "{Date:yyyy-MM-dd}.md";

    /// <summary>
    /// Интервал архивирования в минутах
    /// </summary>
    public int ArchiveIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Интервал создания отчетов в минутах
    /// </summary>
    public int ReportIntervalMinutes { get; set; } = 1440; // 24 часа

    /// <summary>
    /// Максимальное количество сообщений в одном файле
    /// </summary>
    public int MaxMessagesPerFile { get; set; } = 1000;

    /// <summary>
    /// Размер батча для обработки сообщений
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Имя или ID чата для архивирования
    /// </summary>
    public string? TargetChat { get; set; }

    /// <summary>
    /// ID канала для отправки уведомлений об ошибках
    /// </summary>
    public string? ErrorNotificationChat { get; set; }

    /// <summary>
    /// Включить инкрементальную синхронизацию
    /// </summary>
    public bool EnableIncrementalSync { get; set; } = true;

    /// <summary>
    /// Включить ленивую загрузки медиафайлов
    /// </summary>
    public bool EnableLazyMediaDownload { get; set; } = false;

    /// <summary>
    /// Максимальный размер кэша информации о пользователях в записях
    /// </summary>
    public int UserCacheSize { get; set; } = 10000;

    /// <summary>
    /// Включить генерацию индексных файлов
    /// </summary>
    public bool EnableIndexGeneration { get; set; } = true;

    /// <summary>
    /// Включить автоматическую генерацию тегов
    /// </summary>
    public bool EnableAutoTagging { get; set; } = true;

    /// <summary>
    /// Максимальное количество попыток повтора при ошибках
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Базовая задержка для экспоненциального отступа в миллисекундах
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Максимальный размер логфайла в МБ
    /// </summary>
    public int MaxLogFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Количество дней для хранения логов
    /// </summary>
    public int LogRetentionDays { get; set; } = 30;
}