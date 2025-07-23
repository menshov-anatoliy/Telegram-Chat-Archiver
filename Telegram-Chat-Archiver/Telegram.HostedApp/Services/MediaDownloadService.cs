using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для загрузки медиафайлов из Telegram
/// </summary>
public class MediaDownloadService : IMediaDownloadService
{
    private readonly ILogger<MediaDownloadService> _logger;
    private readonly ArchiveConfig _config;

    public MediaDownloadService(ILogger<MediaDownloadService> logger, IOptions<ArchiveConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Загрузить медиафайл и сохранить локально
    /// </summary>
    public async Task<MediaInfo> DownloadMediaAsync(MediaInfo media, string chatTitle, DateTime messageDate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(media.FileName))
            {
                _logger.LogWarning("Имя файла не указано для медиафайла");
                return media;
            }

            var localPath = GetMediaPath(chatTitle, messageDate, media.FileName);
            var directory = Path.GetDirectoryName(localPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Создана папка для медиафайлов: {Directory}", directory);
            }

            // Пока что создаем заглушку файла
            // В реальной реализации здесь будет загрузка через WTelegramClient
            await CreatePlaceholderFileAsync(localPath, media, cancellationToken);
            
            media.LocalPath = localPath;
            _logger.LogInformation("Медиафайл сохранен: {LocalPath}", localPath);
            
            return media;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке медиафайла {FileName}", media.FileName);
            throw;
        }
    }

    /// <summary>
    /// Получить путь для сохранения медиафайла
    /// </summary>
    public string GetMediaPath(string chatTitle, DateTime messageDate, string fileName)
    {
        var sanitizedChatTitle = SanitizeFileName(chatTitle);
        var dateFolder = messageDate.ToString("yyyy-MM");
        var sanitizedFileName = SanitizeFileName(fileName);
        
        return Path.Combine(_config.MediaPath, sanitizedChatTitle, dateFolder, sanitizedFileName);
    }

    /// <summary>
    /// Создание заглушки файла (временно, до реализации реальной загрузки)
    /// </summary>
    private async Task CreatePlaceholderFileAsync(string filePath, MediaInfo media, CancellationToken cancellationToken)
    {
        var content = new StringBuilder();
        content.AppendLine("# Медиафайл - заглушка");
        content.AppendLine();
        content.AppendLine($"**Имя файла:** {media.FileName}");
        content.AppendLine($"**Размер:** {media.FileSize} байт");
        content.AppendLine($"**MIME тип:** {media.MimeType}");
        
        if (media.Width.HasValue && media.Height.HasValue)
            content.AppendLine($"**Разрешение:** {media.Width}x{media.Height}");
            
        if (media.Duration.HasValue)
            content.AppendLine($"**Длительность:** {TimeSpan.FromSeconds(media.Duration.Value):mm\\:ss}");
        
        content.AppendLine();
        content.AppendLine("*Это заглушка. В реальной реализации здесь будет актуальный медиафайл.*");
        
        // Для изображений создаем .md файл с описанием, для остальных - исходное расширение
        var extension = Path.GetExtension(media.FileName);
        if (!string.IsNullOrEmpty(extension) && IsImageFile(extension))
        {
            await File.WriteAllTextAsync(filePath + ".md", content.ToString(), Encoding.UTF8, cancellationToken);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, content.ToString(), Encoding.UTF8, cancellationToken);
        }
    }

    /// <summary>
    /// Проверка, является ли файл изображением
    /// </summary>
    private bool IsImageFile(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff" };
        return imageExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <summary>
    /// Очистка имени файла от недопустимых символов
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "Unknown";
            
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray();
        var sanitized = new StringBuilder();
        
        foreach (char c in fileName)
        {
            if (invalidChars.Contains(c) || c == ' ')
                sanitized.Append('_');
            else
                sanitized.Append(c);
        }
        
        return sanitized.ToString().Trim();
    }
}