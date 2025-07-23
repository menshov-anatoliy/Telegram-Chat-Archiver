using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Создание конфигурации
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Настройка Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("Запуск Telegram Chat Archiver");

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
    /// Создание хоста приложения
    /// </summary>
    /// <param name="args">Аргументы командной строки</param>
    /// <param name="configuration">Конфигурация</param>
    /// <returns>Построитель хоста</returns>
    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Регистрация конфигурации
                services.Configure<TelegramConfig>(configuration.GetSection("TelegramConfig"));
                services.Configure<ArchiveConfig>(configuration.GetSection("ArchiveConfig"));

                // Регистрация сервисов
                services.AddSingleton<ITelegramArchiverService, TelegramArchiverServiceImpl>();
                services.AddHostedService<TelegramArchiverService>();
            });
}
