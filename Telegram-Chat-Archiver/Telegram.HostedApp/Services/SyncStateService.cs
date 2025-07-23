using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для управления состоянием синхронизации
/// </summary>
public class SyncStateService : ISyncStateService
{
    private readonly ILogger<SyncStateService> _logger;
    private readonly ArchiveConfig _config;
    private readonly Dictionary<long, SyncState> _syncStates = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public SyncStateService(
        ILogger<SyncStateService> logger,
        IOptions<ArchiveConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Загрузить состояние синхронизации для чата
    /// </summary>
    public async Task<SyncState> LoadSyncStateAsync(long chatId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_syncStates.TryGetValue(chatId, out var existingState))
            {
                return existingState;
            }

            // Пытаемся загрузить из файла
            var syncState = await LoadFromFileAsync(chatId);
            _syncStates[chatId] = syncState;
            
            return syncState;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Сохранить состояние синхронизации
    /// </summary>
    public async Task SaveSyncStateAsync(SyncState syncState)
    {
        await _semaphore.WaitAsync();
        try
        {
            _syncStates[syncState.ChatId] = syncState;
            await SaveToFileAsync(syncState);
            
            _logger.LogDebug("Состояние синхронизации сохранено для чата {ChatId}", syncState.ChatId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Обновить последний обработанный ID сообщения
    /// </summary>
    public async Task UpdateLastProcessedMessageAsync(long chatId, int messageId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var syncState = await LoadSyncStateAsync(chatId);
            
            if (messageId > syncState.LastProcessedMessageId)
            {
                syncState.LastProcessedMessageId = messageId;
                syncState.LastSyncDate = DateTime.UtcNow;
                syncState.TotalProcessedMessages++;
                
                await SaveSyncStateAsync(syncState);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Сбросить состояние синхронизации для полной пересинхронизации
    /// </summary>
    public async Task ResetSyncStateAsync(long chatId)
    {
        await _semaphore.WaitAsync();
        try
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

            _syncStates[chatId] = syncState;
            await SaveToFileAsync(syncState);
            
            _logger.LogInformation("Состояние синхронизации сброшено для чата {ChatId}", chatId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Получить все состояния синхронизации
    /// </summary>
    public async Task<IEnumerable<SyncState>> GetAllSyncStatesAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _syncStates.Values.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Установить статус синхронизации
    /// </summary>
    public async Task SetSyncStatusAsync(long chatId, SyncStatus status, string? errorMessage = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var syncState = await LoadSyncStateAsync(chatId);
            syncState.Status = status;
            syncState.ErrorMessage = errorMessage;
            syncState.LastSyncDate = DateTime.UtcNow;
            
            await SaveSyncStateAsync(syncState);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<SyncState> LoadFromFileAsync(long chatId)
    {
        try
        {
            var filePath = GetSyncStateFilePath(chatId);
            
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Файл состояния синхронизации не найден для чата {ChatId}, создаем новое состояние", chatId);
                return CreateNewSyncState(chatId);
            }

            var json = await File.ReadAllTextAsync(filePath);
            var syncState = JsonSerializer.Deserialize<SyncState>(json, _jsonOptions);
            
            if (syncState == null)
            {
                _logger.LogWarning("Не удалось десериализовать состояние синхронизации для чата {ChatId}", chatId);
                return CreateNewSyncState(chatId);
            }

            _logger.LogDebug("Состояние синхронизации загружено для чата {ChatId}: последнее сообщение {MessageId}", 
                chatId, syncState.LastProcessedMessageId);
            
            return syncState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке состояния синхронизации для чата {ChatId}", chatId);
            return CreateNewSyncState(chatId);
        }
    }

    private async Task SaveToFileAsync(SyncState syncState)
    {
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
            _logger.LogError(ex, "Ошибка при сохранении состояния синхронизации для чата {ChatId}", syncState.ChatId);
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
}