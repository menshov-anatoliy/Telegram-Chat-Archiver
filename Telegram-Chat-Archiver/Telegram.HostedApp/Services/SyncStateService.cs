using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для управления состоянием синхронизации
/// </summary>
public class SyncStateService(
	ILogger<SyncStateService> logger,
	IOptions<ArchiveConfig> config)
	: ISyncStateService, IDisposable
{
	private readonly ArchiveConfig _config = config.Value;
	private readonly ConcurrentDictionary<long, SyncState> _syncStates = new();
	private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
	private readonly JsonSerializerOptions _jsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};
	private bool _disposed = false;

	/// <summary>
	/// Загрузить состояние синхронизации для чата
	/// </summary>
	public async Task<SyncState> LoadSyncStateAsync(long chatId)
	{
		// Быстрая проверка в памяти без блокировки
		if (_syncStates.TryGetValue(chatId, out var existingState))
		{
			return existingState;
		}

		// Если в памяти нет, загружаем из файла с блокировкой
		var syncState = await LoadFromFileAsync(chatId);
		_syncStates.TryAdd(chatId, syncState);

		return syncState;
	}

	/// <summary>
	/// Сохранить состояние синхронизации
	/// </summary>
	public async Task SaveSyncStateAsync(SyncState syncState)
	{
		// Обновляем кэш в памяти без блокировки
		_syncStates.AddOrUpdate(syncState.ChatId, syncState, (_, _) => syncState);

		// Сохраняем в файл с блокировкой
		await SaveToFileAsync(syncState);

		logger.LogDebug("Состояние синхронизации сохранено для чата {ChatId}", syncState.ChatId);
	}

	/// <summary>
	/// Обновить последний обработанный ID сообщения
	/// </summary>
	public async Task UpdateLastProcessedMessageAsync(long chatId, int messageId)
	{
		var syncState = await LoadSyncStateAsync(chatId);

		if (messageId > syncState.LastProcessedMessageId)
		{
			// Создаем обновленную копию состояния
			var updatedState = new SyncState
			{
				ChatId = syncState.ChatId,
				LastProcessedMessageId = messageId,
				LastSyncDate = DateTime.UtcNow,
				TotalProcessedMessages = syncState.TotalProcessedMessages + 1,
				LastFullSyncDate = syncState.LastFullSyncDate,
				Status = syncState.Status,
				ErrorMessage = syncState.ErrorMessage
			};

			await SaveSyncStateAsync(updatedState);
		}
	}

	/// <summary>
	/// Сбросить состояние синхронизации для полной пересинхронизации
	/// </summary>
	public async Task ResetSyncStateAsync(long chatId)
	{
		var syncState = new SyncState
		{
			ChatId = chatId,
			LastProcessedMessageId = 0,
			LastSyncDate = DateTime.UtcNow,
			TotalProcessedMessages = 0,
			LastFullSyncDate = DateTime.UtcNow,
			Status = SyncStatus.Idle,
			ErrorMessage = null
		};

		// Обновляем кэш в памяти без блокировки
		_syncStates.AddOrUpdate(chatId, syncState, (_, _) => syncState);

		// Сохраняем в файл с блокировкой
		await SaveToFileAsync(syncState);

		logger.LogInformation("Состояние синхронизации сброшено для чата {ChatId}", chatId);
	}

	/// <summary>
	/// Получить все состояния синхронизации
	/// </summary>
	public Task<IEnumerable<SyncState>> GetAllSyncStatesAsync()
	{
		// Возвращаем копию значений из кэша без блокировки
		return Task.FromResult<IEnumerable<SyncState>>(_syncStates.Values.ToList());
	}

	/// <summary>
	/// Установить статус синхронизации
	/// </summary>
	public async Task SetSyncStatusAsync(long chatId, SyncStatus status, string? errorMessage = null)
	{
		var syncState = await LoadSyncStateAsync(chatId);

		// Создаем обновленную копию состояния
		var updatedState = new SyncState
		{
			ChatId = syncState.ChatId,
			LastProcessedMessageId = syncState.LastProcessedMessageId,
			LastSyncDate = DateTime.UtcNow,
			TotalProcessedMessages = syncState.TotalProcessedMessages,
			LastFullSyncDate = syncState.LastFullSyncDate,
			Status = status,
			ErrorMessage = errorMessage
		};

		await SaveSyncStateAsync(updatedState);
	}

	private async Task<SyncState> LoadFromFileAsync(long chatId)
	{
		await _fileSemaphore.WaitAsync();
		try
		{
			var filePath = GetSyncStateFilePath(chatId);

			if (!File.Exists(filePath))
			{
				logger.LogDebug("Файл состояния синхронизации не найден для чата {ChatId}, создаем новое состояние", chatId);
				return CreateNewSyncState(chatId);
			}

			var json = await File.ReadAllTextAsync(filePath);
			var syncState = JsonSerializer.Deserialize<SyncState>(json, _jsonOptions);

			if (syncState == null)
			{
				logger.LogWarning("Не удалось десериализовать состояние синхронизации для чата {ChatId}", chatId);
				return CreateNewSyncState(chatId);
			}

			logger.LogDebug("Состояние синхронизации загружено для чата {ChatId}: последнее сообщение {MessageId}",
				chatId, syncState.LastProcessedMessageId);

			return syncState;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при загрузке состояния синхронизации для чата {ChatId}", chatId);
			return CreateNewSyncState(chatId);
		}
		finally
		{
			_fileSemaphore.Release();
		}
	}

	private async Task SaveToFileAsync(SyncState syncState)
	{
		await _fileSemaphore.WaitAsync();
		try
		{
			var filePath = GetSyncStateFilePath(syncState.ChatId);
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonSerializer.Serialize(syncState, _jsonOptions);
			await File.WriteAllTextAsync(filePath, json);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Ошибка при сохранении состояния синхронизации для чата {ChatId}", syncState.ChatId);
		}
		finally
		{
			_fileSemaphore.Release();
		}
	}

	private string GetSyncStateFilePath(long chatId)
	{
		var fileName = $"sync_state_{chatId}.json";
		return Path.Combine(Path.GetDirectoryName(_config.SyncStatePath) ?? ".", fileName);
	}

	private static SyncState CreateNewSyncState(long chatId)
	{
		return new SyncState
		{
			ChatId = chatId,
			LastProcessedMessageId = 0,
			LastSyncDate = DateTime.UtcNow,
			TotalProcessedMessages = 0,
			LastFullSyncDate = null,
			Status = SyncStatus.Idle,
			ErrorMessage = null
		};
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
			_fileSemaphore?.Dispose();
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Ошибка при освобождении ресурсов SyncStateService");
		}
		finally
		{
			_disposed = true;
		}
	}
}