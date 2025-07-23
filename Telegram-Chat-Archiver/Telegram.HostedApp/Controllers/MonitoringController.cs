using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Controllers;

/// <summary>
/// Контроллер для мониторинга состояния приложения
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly ILogger<MonitoringController> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly IStatisticsService _statisticsService;

    public MonitoringController(
        ILogger<MonitoringController> logger,
        HealthCheckService healthCheckService,
        IStatisticsService statisticsService)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Получить основную информацию о состоянии системы
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var status = new
            {
                Status = healthReport.Status.ToString(),
                TotalDuration = healthReport.TotalDuration,
                Results = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Data = kvp.Value.Data
                    }
                ),
                Timestamp = DateTime.UtcNow
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статуса системы");
            return StatusCode(500, new { Error = "Ошибка при получении статуса системы" });
        }
    }

    /// <summary>
    /// Получить статистику работы системы
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var messageStats = await _statisticsService.GetMessageTypeStatisticsAsync();
            var authorStats = await _statisticsService.GetAuthorStatisticsAsync();
            
            var statistics = new
            {
                MessageStatistics = messageStats,
                AuthorStatistics = authorStats,
                Timestamp = DateTime.UtcNow
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики");
            return StatusCode(500, new { Error = "Ошибка при получении статистики" });
        }
    }

    /// <summary>
    /// Получить информацию о версии и конфигурации
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        try
        {
            var info = new
            {
                Version = "1.0.0", // Можно получать из Assembly
                Framework = ".NET 8.0",
                Environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development",
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                StartTime = Process.GetCurrentProcess().StartTime,
                Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                WorkingSet = GC.GetTotalMemory(false),
                Timestamp = DateTime.UtcNow
            };

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении информации о системе");
            return StatusCode(500, new { Error = "Ошибка при получении информации о системе" });
        }
    }

    /// <summary>
    /// Получить метрики производительности в формате Prometheus
    /// </summary>
    [HttpGet("metrics")]
    [Produces("text/plain")]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var metrics = new List<string>();
            
            // Основные метрики приложения
            var process = Process.GetCurrentProcess();
            metrics.Add($"telegram_archiver_memory_usage_bytes {process.WorkingSet64}");
            metrics.Add($"telegram_archiver_cpu_usage_seconds_total {process.TotalProcessorTime.TotalSeconds}");
            metrics.Add($"telegram_archiver_threads_count {process.Threads.Count}");
            metrics.Add($"telegram_archiver_handles_count {process.HandleCount}");
            metrics.Add($"telegram_archiver_uptime_seconds {(DateTime.Now - process.StartTime).TotalSeconds}");
            
            // Статистика сообщений
            var messageStats = await _statisticsService.GetMessageTypeStatisticsAsync();
            foreach (var stat in messageStats)
            {
                metrics.Add($"telegram_archiver_messages_total{{type=\"{stat.Key}\"}} {stat.Value}");
            }
            
            // Health check статусы
            var healthReport = await _healthCheckService.CheckHealthAsync();
            foreach (var check in healthReport.Entries)
            {
                var statusValue = check.Value.Status switch
                {
                    HealthStatus.Healthy => 1,
                    HealthStatus.Degraded => 0.5,
                    HealthStatus.Unhealthy => 0,
                    _ => 0
                };
                metrics.Add($"telegram_archiver_health_check{{check=\"{check.Key}\"}} {statusValue}");
            }
            
            // Временная метка последнего обновления
            metrics.Add($"telegram_archiver_last_update_timestamp {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            
            var result = string.Join("\n", metrics) + "\n";
            return Content(result, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации метрик");
            return StatusCode(500, "Ошибка при генерации метрик");
        }
    }
}