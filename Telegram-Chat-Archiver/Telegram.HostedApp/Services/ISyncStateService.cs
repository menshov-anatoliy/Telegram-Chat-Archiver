using Telegram.HostedApp.Models;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса для управления состоянием синхронизации
/// </summary>
public interface ISyncStateService : IDisposable
{
    /// <summary>
    /// Загрузить состояние синхронизации для чата
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <returns>Состояние синхронизации или новое состояние, если не найдено</returns>
    Task<SyncState> LoadSyncStateAsync(long chatId);

    /// <summary>
    /// Сохранить состояние синхронизации
    /// </summary>
    /// <param name="syncState">Состояние синхронизации</param>
    Task SaveSyncStateAsync(SyncState syncState);

    /// <summary>
    /// Обновить последний обработанный ID сообщения
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="messageId">ID сообщения</param>
    Task UpdateLastProcessedMessageAsync(long chatId, int messageId);

    /// <summary>
    /// Сбросить состояние синхронизации для полной пересинхронизации
    /// </summary>
    /// <param name="chatId">ID чата</param>
    Task ResetSyncStateAsync(long chatId);

    /// <summary>
    /// Получить все состояния синхронизации
    /// </summary>
    /// <returns>Список всех состояний синхронизации</returns>
    Task<IEnumerable<SyncState>> GetAllSyncStatesAsync();

    /// <summary>
    /// Установить статус синхронизации
    /// </summary>
    /// <param name="chatId">ID чата</param>
    /// <param name="status">Новый статус</param>
    /// <param name="errorMessage">Сообщение об ошибке (если статус - Error)</param>
    Task SetSyncStatusAsync(long chatId, SyncStatus status, string? errorMessage = null);
}