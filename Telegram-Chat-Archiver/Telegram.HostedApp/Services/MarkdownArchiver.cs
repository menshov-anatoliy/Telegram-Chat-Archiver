using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Сервис для инкрементального архивирования сообщений в Markdown формате
/// Обеспечивает потокобезопасную запись сообщений в файлы с группировкой по датам
/// </summary>
public class MarkdownArchiver
{
	private readonly ILogger<MarkdownArchiver> _logger;
	private readonly ArchiveConfig _config;
	private readonly object _lockObject = new();

	public MarkdownArchiver(ILogger<MarkdownArchiver> logger, IOptions<ArchiveConfig> config)
	{
		_logger = logger;
		_config = config.Value;
	}

	/// <summary>
	/// Добавить сообщение в архив (инкрементально)
	/// Использует блокировку для обеспечения потокобезопасности
	/// </summary>
	/// <param name="message">Сообщение для добавления</param>
	/// <param name="chatTitle">Название чата</param>
	/// <param name="cancellationToken">Токен отмены</param>
	public Task AppendMessageAsync(ChatMessage message, string chatTitle, CancellationToken cancellationToken = default)
	{
		try
		{
			var filePath = GetMarkdownFilePath(chatTitle, message.Date);
			var directory = Path.GetDirectoryName(filePath);

			// Создаем директорию если её нет
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
				_logger.LogDebug("Создана папка: {Directory}", directory);
			}

			var messageMarkdown = FormatMessage(message);

			// Потокобезопасная запись
			lock (_lockObject)
			{
				// Если файл не существует, создаем его с заголовком
				if (!File.Exists(filePath))
				{
					var header = GenerateFileHeader(chatTitle, message.Date);
					File.WriteAllText(filePath, header + messageMarkdown, Encoding.UTF8);
				}
				else
				{
					// Дописываем сообщение в конец файла
					File.AppendAllText(filePath, messageMarkdown, Encoding.UTF8);
				}
			}

			_logger.LogDebug("Добавлено сообщение {MessageId} в файл: {FilePath}", message.Id, filePath);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при добавлении сообщения {MessageId} в архив", message.Id);
			throw;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Получить путь к Markdown файлу для указанной даты
	/// Создает файлы с именами по дате (ГГГГ-ММ-ДД.md)
	/// </summary>
	/// <param name="chatTitle">Название чата</param>
	/// <param name="date">Дата</param>
	/// <returns>Путь к файлу</returns>
	public string GetMarkdownFilePath(string chatTitle, DateTime date)
	{
		var sanitizedChatTitle = SanitizeFileName(chatTitle);
		var fileName = $"{date:yyyy-MM-dd}.md";

		return Path.Combine(_config.OutputPath, sanitizedChatTitle, fileName);
	}

	/// <summary>
	/// Генерация заголовка файла
	/// </summary>
	private string GenerateFileHeader(string chatTitle, DateTime date)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"# {EscapeMarkdown(chatTitle)}");
		sb.AppendLine();
		sb.AppendLine($"**Дата:** {date:yyyy-MM-dd}");
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

		// Информация о редактировании
		if (message.IsEdited)
			sb.AppendLine($"*✏️ Сообщение отредактировано: {message.EditDate:HH:mm:ss}*");

		// Информация о пересылке
		if (message.ForwardInfo != null)
		{
			sb.AppendLine($"*📤 Переслано от: {EscapeMarkdown(message.ForwardInfo.FromName ?? "Неизвестно")} ({message.ForwardInfo.OriginalDate:HH:mm:ss})*");
		}

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