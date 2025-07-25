using Microsoft.Extensions.Options;
using System.Text;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для работы с Markdown файлами
/// </summary>
public class MarkdownService(ILogger<MarkdownService> logger, IOptions<ArchiveConfig> config)
	: IMarkdownService
{
	private readonly ArchiveConfig _config = config.Value;

	/// <summary>
	/// Сохранить сообщения в Markdown файл
	/// </summary>
	public async Task SaveMessagesAsync(IEnumerable<ChatMessage> messages, string chatTitle, DateTime date, CancellationToken cancellationToken = default)
	{
		try
		{
			var filePath = GetMarkdownFilePath(chatTitle, date);
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
				logger.LogDebug("Создана папка: {Directory}", directory);
			}

			var markdown = GenerateMarkdownContent(messages, chatTitle, date);

			await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8, cancellationToken);
			logger.LogInformation("Сохранено {MessageCount} сообщений в файл: {FilePath}", messages.Count(), filePath);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при сохранении сообщений в Markdown файл для чата {ChatTitle}", chatTitle);
			throw;
		}
	}

	/// <summary>
	/// Добавить сообщение в существующий Markdown файл
	/// </summary>
	public async Task AppendMessageAsync(ChatMessage message, string chatTitle, CancellationToken cancellationToken = default)
	{
		try
		{
			var filePath = GetMarkdownFilePath(chatTitle, message.Date);
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
				logger.LogDebug("Создана папка: {Directory}", directory);
			}

			var messageMarkdown = FormatMessage(message);

			// Если файл не существует, создаем его с заголовком
			if (!File.Exists(filePath))
			{
				var header = GenerateFileHeader(chatTitle, message.Date);
				await File.WriteAllTextAsync(filePath, header + messageMarkdown, Encoding.UTF8, cancellationToken);
			}
			else
			{
				await File.AppendAllTextAsync(filePath, messageMarkdown, Encoding.UTF8, cancellationToken);
			}

			logger.LogDebug("Добавлено сообщение {MessageId} в файл: {FilePath}", message.Id, filePath);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при добавлении сообщения {MessageId} в Markdown файл", message.Id);
			throw;
		}
	}

	/// <summary>
	/// Получить путь к Markdown файлу для указанной даты
	/// </summary>
	public string GetMarkdownFilePath(string chatTitle, DateTime date)
	{
		var sanitizedChatTitle = SanitizeFileName(chatTitle);
		var fileName = _config.FileNameFormat.Replace("{Date:yyyy-MM-dd}", date.ToString("yyyy-MM-dd"));

		return Path.Combine(_config.OutputPath, sanitizedChatTitle, fileName);
	}

	/// <summary>
	/// Генерация содержимого Markdown файла
	/// </summary>
	private string GenerateMarkdownContent(IEnumerable<ChatMessage> messages, string chatTitle, DateTime date)
	{
		var sb = new StringBuilder();

		// Заголовок файла
		sb.AppendLine(GenerateFileHeader(chatTitle, date));

		// Сообщения
		foreach (var message in messages.OrderBy(m => m.Date))
		{
			sb.AppendLine(FormatMessage(message));
		}

		return sb.ToString();
	}

	/// <summary>
	/// Генерация заголовка файла
	/// </summary>
	private string GenerateFileHeader(string chatTitle, DateTime date)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"# {EscapeMarkdown(chatTitle)}");
		sb.AppendLine();
		sb.AppendLine($"**Дата:** {date.ToLocalTime():yyyy-MM-dd}");
		sb.AppendLine();
		sb.AppendLine("---");
		sb.AppendLine();

		return sb.ToString();
	}

	/// <summary>
	/// Форматирование сообщения в Markdown
	/// </summary>
	private string FormatMessage(ChatMessage message)
	{
		var sb = new StringBuilder();

		// Временная метка и автор
		sb.AppendLine($"## {message.Date:HH:mm:ss} - {EscapeMarkdown(message.AuthorName ?? "Неизвестный")}");
		sb.AppendLine();

		// Содержимое сообщения в зависимости от типа
		switch (message.Type)
		{
			case MessageType.Text:
				if (!string.IsNullOrEmpty(message.Text))
					sb.AppendLine(EscapeMarkdown(message.Text));
				break;

			case MessageType.Photo:
				sb.AppendLine("📷 **Изображение**");
				if (message.Media?.LocalPath != null)
					sb.AppendLine($"![Изображение]({GetRelativeMediaPath(message.Media.LocalPath)})");
				if (!string.IsNullOrEmpty(message.Text))
					sb.AppendLine($"*Описание:* {EscapeMarkdown(message.Text)}");
				break;

			case MessageType.Document:
				sb.AppendLine("📄 **Документ**");
				if (message.Media != null)
				{
					sb.AppendLine($"*Файл:* {EscapeMarkdown(message.Media.FileName ?? "Неизвестно")}");
					if (message.Media.FileSize.HasValue)
						sb.AppendLine($"*Размер:* {FormatFileSize(message.Media.FileSize.Value)}");
					if (message.Media.LocalPath != null)
						sb.AppendLine($"[Скачать файл]({GetRelativeMediaPath(message.Media.LocalPath)})");
				}
				if (!string.IsNullOrEmpty(message.Text))
					sb.AppendLine($"*Описание:* {EscapeMarkdown(message.Text)}");
				break;

			case MessageType.Voice:
				sb.AppendLine("🎵 **Голосовое сообщение**");
				if (message.Media?.Duration.HasValue == true)
					sb.AppendLine($"*Длительность:* {TimeSpan.FromSeconds(message.Media.Duration.Value):mm\\:ss}");
				if (message.Media?.LocalPath != null)
					sb.AppendLine($"[Аудиофайл]({GetRelativeMediaPath(message.Media.LocalPath)})");
				break;

			case MessageType.Video:
				sb.AppendLine("🎬 **Видео**");
				if (message.Media != null)
				{
					if (message.Media.Duration.HasValue)
						sb.AppendLine($"*Длительность:* {TimeSpan.FromSeconds(message.Media.Duration.Value):mm\\:ss}");
					if (message.Media.Width.HasValue && message.Media.Height.HasValue)
						sb.AppendLine($"*Разрешение:* {message.Media.Width}x{message.Media.Height}");
					if (message.Media.LocalPath != null)
						sb.AppendLine($"[Видеофайл]({GetRelativeMediaPath(message.Media.LocalPath)})");
				}
				if (!string.IsNullOrEmpty(message.Text))
					sb.AppendLine($"*Описание:* {EscapeMarkdown(message.Text)}");
				break;

			case MessageType.Sticker:
				sb.AppendLine("😄 **Стикер**");
				if (message.Media?.LocalPath != null)
					sb.AppendLine($"![Стикер]({GetRelativeMediaPath(message.Media.LocalPath)})");
				break;

			default:
				sb.AppendLine("❓ **Неизвестный тип сообщения**");
				if (!string.IsNullOrEmpty(message.Text))
					sb.AppendLine(EscapeMarkdown(message.Text));
				break;
		}

		// Ответ на сообщение
		if (message.ReplyToMessageId.HasValue)
			sb.AppendLine($"*↩️ Ответ на сообщение ID: {message.ReplyToMessageId.Value}*");

		sb.AppendLine();
		sb.AppendLine("---");
		sb.AppendLine();

		return sb.ToString();
	}

	/// <summary>
	/// Экранирование специальных символов Markdown
	/// </summary>
	private string EscapeMarkdown(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return text
			.Replace("\\", "\\\\")
			.Replace("*", "\\*")
			.Replace("_", "\\_")
			.Replace("[", "\\[")
			.Replace("]", "\\]")
			.Replace("(", "\\(")
			.Replace(")", "\\)")
			.Replace("#", "\\#")
			.Replace("+", "\\+")
			.Replace("-", "\\-")
			.Replace(".", "\\.")
			.Replace("!", "\\!");
	}

	/// <summary>
	/// Очистка имени файла от недопустимых символов
	/// </summary>
	private string SanitizeFileName(string fileName)
	{
		if (string.IsNullOrEmpty(fileName))
			return "Unknown";

		var invalidChars = Path.GetInvalidFileNameChars();
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

	/// <summary>
	/// Форматирование размера файла в читаемый вид
	/// </summary>
	private string FormatFileSize(long bytes)
	{
		string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		double len = bytes;
		int order = 0;

		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}

		return $"{len:0.##} {sizes[order]}";
	}

	/// <summary>
	/// Получение относительного пути к медиафайлу
	/// </summary>
	private string GetRelativeMediaPath(string localPath)
	{
		// Возвращаем относительный путь от папки архива к медиафайлу
		var archiveDir = Path.GetFullPath(_config.OutputPath);
		var mediaFullPath = Path.GetFullPath(localPath);

		return Path.GetRelativePath(archiveDir, mediaFullPath).Replace("\\", "/");
	}
}