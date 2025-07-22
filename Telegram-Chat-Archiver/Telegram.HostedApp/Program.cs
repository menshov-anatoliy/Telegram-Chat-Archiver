using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace Telegram.HostedApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ArchiverService>();
                    services.AddSingleton<IFileStorageService, FileStorageService>();
                    services.AddSingleton<ITranscriptionService, TranscriptionService>();
                    services.AddSingleton<ITelegramBotClient>(sp =>
                    {
                        var botToken = sp.GetRequiredService<IConfiguration>()["Telegram:BotToken"];
                        return new TelegramBotClient(botToken);
                    });
                });
    }
}
