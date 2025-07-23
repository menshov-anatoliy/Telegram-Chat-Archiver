using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests;

[TestClass]
public sealed class ConfigurationTests
{
    [TestMethod]
    public void TelegramConfig_ShouldHaveCorrectProperties()
    {
        // Arrange
        var config = new TelegramConfig
        {
            ApiId = 12345,
            ApiHash = "test_hash",
            PhoneNumber = "+1234567890",
            SessionFile = "test_session.dat"
        };

        // Assert
        Assert.AreEqual(12345, config.ApiId);
        Assert.AreEqual("test_hash", config.ApiHash);
        Assert.AreEqual("+1234567890", config.PhoneNumber);
        Assert.AreEqual("test_session.dat", config.SessionFile);
    }

    [TestMethod]
    public void ArchiveConfig_ShouldHaveCorrectProperties()
    {
        // Arrange
        var config = new ArchiveConfig
        {
            OutputPath = "test_archives",
            MediaPath = "test_media",
            FileNameFormat = "test_format.md",
            ArchiveIntervalMinutes = 30,
            MaxMessagesPerFile = 500,
            TargetChat = "test_chat",
            ErrorNotificationChat = "error_chat"
        };

        // Assert
        Assert.AreEqual("test_archives", config.OutputPath);
        Assert.AreEqual("test_media", config.MediaPath);
        Assert.AreEqual("test_format.md", config.FileNameFormat);
        Assert.AreEqual(30, config.ArchiveIntervalMinutes);
        Assert.AreEqual(500, config.MaxMessagesPerFile);
        Assert.AreEqual("test_chat", config.TargetChat);
        Assert.AreEqual("error_chat", config.ErrorNotificationChat);
    }

    [TestMethod]
    public void ServiceRegistration_ShouldRegisterAllServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"TelegramConfig:ApiId", "12345"},
                {"TelegramConfig:ApiHash", "test_hash"},
                {"TelegramConfig:PhoneNumber", "+1234567890"},
                {"ArchiveConfig:OutputPath", "test_archives"}
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<TelegramConfig>(configuration.GetSection("TelegramConfig"));
        services.Configure<ArchiveConfig>(configuration.GetSection("ArchiveConfig"));
        services.AddSingleton<IMarkdownService, MarkdownService>();
        services.AddSingleton<IMediaDownloadService, MediaDownloadService>();
        services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
        services.AddSingleton<ITelegramArchiverService, TelegramArchiverServiceImpl>();
        services.AddLogging();

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var telegramConfig = serviceProvider.GetService<IOptions<TelegramConfig>>();
        var archiveConfig = serviceProvider.GetService<IOptions<ArchiveConfig>>();
        var archiverService = serviceProvider.GetService<ITelegramArchiverService>();
        var markdownService = serviceProvider.GetService<IMarkdownService>();
        var mediaService = serviceProvider.GetService<IMediaDownloadService>();
        var notificationService = serviceProvider.GetService<ITelegramNotificationService>();

        Assert.IsNotNull(telegramConfig);
        Assert.IsNotNull(archiveConfig);
        Assert.IsNotNull(archiverService);
        Assert.IsNotNull(markdownService);
        Assert.IsNotNull(mediaService);
        Assert.IsNotNull(notificationService);
        Assert.AreEqual(12345, telegramConfig.Value.ApiId);
        Assert.AreEqual("test_hash", telegramConfig.Value.ApiHash);
    }
}
