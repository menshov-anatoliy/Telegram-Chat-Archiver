using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using Telegram.HostedApp.Services.Interfaces;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для сбора и управления статистикой
/// </summary>
public class StatisticsService(
	ILogger<StatisticsService> logger,
	IOptions<ArchiveConfig> config)
	: IStatisticsService
{
	private readonly ArchiveConfig _config = config.Value;

	private readonly ProcessingStatistics _statistics = new()
	{
		SessionStart = DateTime.UtcNow,
		LastUpdate = DateTime.UtcNow
	};

	private readonly SemaphoreSlim _semaphore = new(1, 1);

	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private bool _disposed;

	/// <summary>
	/// Записать обработку сообщения
	/// </summary>
	public async Task RecordMessageProcessedAsync(ChatMessage message, double processingTimeMs)
	{
		await _semaphore.WaitAsync();
		try
		{
			_statistics.TotalMessagesProcessed++;
			_statistics.LastUpdate = DateTime.UtcNow;

			// Обновляем среднее время обработки
			UpdateAverageProcessingTime(processingTimeMs);

			// Обновляем статистику по типам сообщений
			if (_statistics.MessageTypeStats.TryAdd(message.Type, 1) == false)
			{
				_statistics.MessageTypeStats[message.Type]++;
			}

			// Обновляем статистику по авторам
			await UpdateAuthorStatisticsAsync(message);

			logger.LogDebug("Записана обработка сообщения {MessageId} за {ProcessingTime}мс",
				message.Id, processingTimeMs);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Записать загрузку медиафайла
	/// </summary>
	public async Task RecordMediaDownloadAsync(long fileSize)
	{
		await _semaphore.WaitAsync();
		try
		{
			_statistics.MediaFilesDownloaded++;
			_statistics.TotalDownloadedSizeBytes += fileSize;
			_statistics.LastUpdate = DateTime.UtcNow;

			logger.LogDebug("Записана загрузка медиафайла размером {FileSize} байт", fileSize);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Записать ошибку
	/// </summary>
	public async Task RecordErrorAsync(string error, Exception? exception = null)
	{
		await _semaphore.WaitAsync();
		try
		{
			_statistics.ErrorCount++;
			_statistics.LastUpdate = DateTime.UtcNow;

			logger.LogDebug("Записана ошибка: {Error}", error);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Записать создание архивного файла
	/// </summary>
	public async Task RecordArchiveCreatedAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			_statistics.ArchiveFilesCreated++;
			_statistics.LastArchiveTime = DateTime.UtcNow;
			_statistics.LastUpdate = DateTime.UtcNow;

			logger.LogDebug("Записано создание архивного файла");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Получить текущую статистику
	/// </summary>
	public async Task<ProcessingStatistics> GetStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			return new ProcessingStatistics
			{
				SessionStart = _statistics.SessionStart,
				LastUpdate = _statistics.LastUpdate,
				TotalMessagesProcessed = _statistics.TotalMessagesProcessed,
				MediaFilesDownloaded = _statistics.MediaFilesDownloaded,
				TotalDownloadedSizeBytes = _statistics.TotalDownloadedSizeBytes,
				ErrorCount = _statistics.ErrorCount,
				AverageProcessingTimeMs = _statistics.AverageProcessingTimeMs,
				MessageTypeStats = new Dictionary<MessageType, long>(_statistics.MessageTypeStats),
				AuthorStats = new Dictionary<long, AuthorStats>(_statistics.AuthorStats),
				LastArchiveTime = _statistics.LastArchiveTime,
				ArchiveFilesCreated = _statistics.ArchiveFilesCreated
			};
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Сбросить статистику сессии
	/// </summary>
	public async Task ResetSessionStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			_statistics.SessionStart = DateTime.UtcNow;
			_statistics.LastUpdate = DateTime.UtcNow;
			_statistics.TotalMessagesProcessed = 0;
			_statistics.MediaFilesDownloaded = 0;
			_statistics.TotalDownloadedSizeBytes = 0;
			_statistics.ErrorCount = 0;
			_statistics.AverageProcessingTimeMs = 0;
			_statistics.MessageTypeStats.Clear();
			_statistics.AuthorStats.Clear();
			_statistics.LastArchiveTime = null;
			_statistics.ArchiveFilesCreated = 0;

			logger.LogInformation("Статистика сессии сброшена");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Получить статистику по авторам
	/// </summary>
	public async Task<Dictionary<long, AuthorStats>> GetAuthorStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			return new Dictionary<long, AuthorStats>(_statistics.AuthorStats);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Получить статистику по типам сообщений
	/// </summary>
	public async Task<Dictionary<MessageType, long>> GetMessageTypeStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			return new Dictionary<MessageType, long>(_statistics.MessageTypeStats);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Сохранить статистику в файл
	/// </summary>
	public async Task SaveStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			var filePath = GetStatisticsFilePath();
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonSerializer.Serialize(_statistics, _jsonOptions);
			await File.WriteAllTextAsync(filePath, json);

			logger.LogDebug("Статистика сохранена в файл {FilePath}", filePath);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при сохранении статистики");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <summary>
	/// Загрузить статистику из файла
	/// </summary>
	public async Task LoadStatisticsAsync()
	{
		await _semaphore.WaitAsync();
		try
		{
			var filePath = GetStatisticsFilePath();

			if (!File.Exists(filePath))
			{
				logger.LogDebug("Файл статистики не найден: {FilePath}", filePath);
				return;
			}

			var json = await File.ReadAllTextAsync(filePath);
			var loadedStats = JsonSerializer.Deserialize<ProcessingStatistics>(json, _jsonOptions);

			if (loadedStats != null)
			{
				// Копируем только постоянные данные, сессионные данные оставляем текущими
				_statistics.MessageTypeStats.Clear();
				foreach (var kvp in loadedStats.MessageTypeStats)
				{
					_statistics.MessageTypeStats[kvp.Key] = kvp.Value;
				}

				_statistics.AuthorStats.Clear();
				foreach (var kvp in loadedStats.AuthorStats)
				{
					_statistics.AuthorStats[kvp.Key] = kvp.Value;
				}

				_statistics.ArchiveFilesCreated += loadedStats.ArchiveFilesCreated;

				logger.LogInformation("Статистика загружена из файла");
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при загрузке статистики");
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private void UpdateAverageProcessingTime(double newTime)
	{
		if (_statistics.TotalMessagesProcessed == 1)
		{
			_statistics.AverageProcessingTimeMs = newTime;
		}
		else
		{
			// Вычисляем скользящее среднее
			_statistics.AverageProcessingTimeMs =
				(_statistics.AverageProcessingTimeMs * (_statistics.TotalMessagesProcessed - 1) + newTime)
				/ _statistics.TotalMessagesProcessed;
		}
	}

	private Task UpdateAuthorStatisticsAsync(ChatMessage message)
	{
		var authorId = message.AuthorId;

		if (!_statistics.AuthorStats.TryGetValue(authorId, out var authorStats))
		{
			authorStats = new AuthorStats
			{
				AuthorId = authorId,
				AuthorName = message.AuthorName,
				MessageCount = 0,
				MediaCount = 0,
				FirstMessageDate = message.Date,
				LastMessageDate = message.Date
			};
			_statistics.AuthorStats[authorId] = authorStats;
		}

		authorStats.MessageCount++;
		authorStats.LastMessageDate = message.Date;

		// Обновляем имя автора, если оно изменилось
		if (!string.IsNullOrEmpty(message.AuthorName))
		{
			authorStats.AuthorName = message.AuthorName;
		}

		// Считаем медиафайлы
		if (message.Media != null || message.MediaGroup?.Any() == true)
		{
			authorStats.MediaCount++;
		}

		// Обновляем дату первого сообщения, если необходимо
		if (message.Date < authorStats.FirstMessageDate)
		{
			authorStats.FirstMessageDate = message.Date;
		}

		return Task.CompletedTask;
	}

	private string GetStatisticsFilePath()
	{
		return Path.Combine(Path.GetDirectoryName(_config.SyncStatePath) ?? ".", "statistics.json");
	}

	/// <summary>
	/// Освобождение ресурсов
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		try
		{
			_semaphore?.Dispose();
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Ошибка при освобождении ресурсов StatisticsService");
		}
		finally
		{
			_disposed = true;
		}
	}
}