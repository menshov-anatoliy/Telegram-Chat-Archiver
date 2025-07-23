using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Telegram.Bot.Exceptions;
using Telegram.HostedApp.Configuration;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса для выполнения операций с повторными попытками
/// </summary>
public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly ArchiveConfig _config;

    public RetryService(
        ILogger<RetryService> logger,
        IOptions<ArchiveConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    /// <summary>
    /// Выполнить операцию с повторными попытками
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            operation,
            _config.MaxRetryAttempts,
            _config.BaseRetryDelayMs,
            cancellationToken);
    }

    /// <summary>
    /// Выполнить операцию с повторными попытками (без возвращаемого значения)
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // заглушка для возврата значения
        }, cancellationToken);
    }

    /// <summary>
    /// Выполнить операцию с повторными попытками и кастомными параметрами
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        int baseDelayMs,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxAttempts)
        {
            attempt++;
            
            try
            {
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Операция выполнена успешно с {Attempt} попытки", attempt);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (!IsTransientException(ex) || attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Операция завершилась неудачей после {Attempt} попыток", attempt);
                    throw;
                }

                var delay = CalculateDelay(attempt, baseDelayMs);
                
                _logger.LogWarning(ex, 
                    "Попытка {Attempt} из {MaxAttempts} завершилась неудачей. " +
                    "Повтор через {Delay}мс. Ошибка: {Error}",
                    attempt, maxAttempts, delay, ex.Message);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Операция отменена во время ожидания повтора");
                    throw;
                }
            }
        }

        // Этот код никогда не должен выполниться, но для безопасности
        throw lastException ?? new InvalidOperationException("Неожиданная ошибка в RetryService");
    }

    /// <summary>
    /// Проверить, является ли исключение временным (подлежащим повтору)
    /// </summary>
    public bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            // Сетевые ошибки
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            
            // HTTP ошибки сервера
            HttpListenerException httpEx => httpEx.ErrorCode >= 500,
            
            // Telegram API ошибки
            ApiRequestException apiEx => IsTransientApiException(apiEx),
            
            // Файловые операции (временная недоступность)
            IOException => true,
            UnauthorizedAccessException => true,
            
            // Остальные исключения не повторяем
            _ => false
        };
    }

    private static bool IsTransientApiException(ApiRequestException apiException)
    {
        return apiException.ErrorCode switch
        {
            // Rate limiting
            429 => true,
            
            // Gateway errors
            502 or 504 => true,
            
            // Временная недоступность
            503 => true,
            
            // Server errors
            >= 500 and <= 599 => true,
            
            // Остальные API ошибки не повторяем
            _ => false
        };
    }

    private static int CalculateDelay(int attempt, int baseDelayMs)
    {
        // Экспоненциальная задержка с jitter для предотвращения thundering herd
        var exponentialDelay = baseDelayMs * Math.Pow(2, attempt - 1);
        
        // Добавляем случайный jitter (±25%)
        var random = new Random();
        var jitter = random.NextDouble() * 0.5 + 0.75; // 0.75 - 1.25
        
        var finalDelay = (int)(exponentialDelay * jitter);
        
        // Ограничиваем максимальную задержку
        const int maxDelayMs = 60000; // 1 минута
        return Math.Min(finalDelay, maxDelayMs);
    }
}