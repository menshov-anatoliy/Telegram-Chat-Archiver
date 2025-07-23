using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Serilog;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;
using Telegram.HostedApp.HealthChecks;

namespace Telegram.HostedApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Проверяем аргументы командной строки для health check
        if (args.Contains("--health-check"))
        {
            Environment.Exit(await PerformHealthCheckAsync() ? 0 : 1);
        }

        // Создание конфигурации
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Настройка Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Запуск Telegram Chat Archiver с расширенной функциональностью");

            // Создание и запуск хоста
            var host = CreateHostBuilder(args, configuration).Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Критическая ошибка при запуске приложения");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Выполнение быстрой проверки состояния для Docker health check
    /// </summary>
    /// <returns>True если система здорова, иначе false</returns>
    static async Task<bool> PerformHealthCheckAsync()
    {
        try
        {
            // Базовые проверки для быстрого health check
            var archivesPath = Environment.GetEnvironmentVariable("ArchiveConfig__OutputPath") ?? "archives";
            var mediaPath = Environment.GetEnvironmentVariable("ArchiveConfig__MediaPath") ?? "media";

            // Проверяем доступность директорий
            if (!Directory.Exists(archivesPath) || !Directory.Exists(mediaPath))
            {
                return false;
            }

            // Проверяем возможность записи
            var testFile = Path.Combine(archivesPath, $"health_test_{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Создание хоста приложения
    /// </summary>
    /// <param name="args">Аргументы командной строки</param>
    /// <param name="configuration">Конфигурация</param>
    /// <returns>Построитель хоста</returns>
    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    // Middleware для обработки исключений
                    app.UseExceptionHandler("/error");
                    
                    // Статические файлы
                    app.UseDefaultFiles();
                    app.UseStaticFiles();
                    
                    // Health check endpoints
                    app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        ResponseWriter = WriteHealthCheckResponse
                    });
                    
                    app.UseHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("live"),
                        ResponseWriter = WriteHealthCheckResponse
                    });
                    
                    app.UseHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("ready"),
                        ResponseWriter = WriteHealthCheckResponse
                    });

                    app.UseHealthChecks("/health/startup", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                    {
                        Predicate = check => check.Tags.Contains("startup"),
                        ResponseWriter = WriteHealthCheckResponse
                    });

                    // Routing и контроллеры
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapFallbackToFile("index.html");
                    });
                });

                webBuilder.ConfigureServices(services =>
                {
                    // ASP.NET Core services
                    services.AddControllers();
                    
                    // Health checks
                    services.AddHealthChecks()
                        .AddCheck<TelegramConnectionHealthCheck>("telegram", tags: new[] { "live", "ready" })
                        .AddCheck<FileSystemHealthCheck>("filesystem", tags: new[] { "live", "ready", "startup" })
                        .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
                        .AddCheck<SystemResourcesHealthCheck>("resources", tags: new[] { "live" });
                });
            })
            .ConfigureServices((context, services) =>
            {
                // Регистрация конфигурации
                services.Configure<TelegramConfig>(configuration.GetSection("TelegramConfig"));
                services.Configure<BotConfig>(configuration.GetSection("BotConfig"));
                services.Configure<ArchiveConfig>(configuration.GetSection("ArchiveConfig"));

                // Регистрация базовых сервисов
                services.AddSingleton<IMarkdownService, MarkdownService>();
                services.AddSingleton<IMediaDownloadService, MediaDownloadService>();
                services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
                
                // Регистрация новых сервисов
                services.AddSingleton<ITelegramBotService, TelegramBotService>();
                services.AddSingleton<ISyncStateService, SyncStateService>();
                services.AddSingleton<IStatisticsService, StatisticsService>();
                services.AddSingleton<IRetryService, RetryService>();
                
                // Основной сервис архивирования
                services.AddSingleton<ITelegramArchiverService, TelegramArchiverServiceImpl>();
                services.AddHostedService<TelegramArchiverService>();
            });

    /// <summary>
    /// Кастомная функция для записи ответа health check
    /// </summary>
    static async Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = healthReport.Status.ToString(),
            totalDuration = healthReport.TotalDuration.TotalMilliseconds,
            results = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    description = kvp.Value.Description,
                    duration = kvp.Value.Duration.TotalMilliseconds,
                    data = kvp.Value.Data
                }
            ),
            timestamp = DateTime.UtcNow
        };

        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
