namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса для выполнения операций с повторными попытками
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Выполнить операцию с повторными попытками
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции</returns>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполнить операцию с повторными попытками (без возвращаемого значения)
    /// </summary>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполнить операцию с повторными попытками и кастомными параметрами
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="operation">Операция для выполнения</param>
    /// <param name="maxAttempts">Максимальное количество попыток</param>
    /// <param name="baseDelayMs">Базовая задержка в миллисекундах</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Результат операции</returns>
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        int baseDelayMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, является ли исключение временным (подлежащим повтору)
    /// </summary>
    /// <param name="exception">Исключение для проверки</param>
    /// <returns>True, если исключение временное</returns>
    bool IsTransientException(Exception exception);
}