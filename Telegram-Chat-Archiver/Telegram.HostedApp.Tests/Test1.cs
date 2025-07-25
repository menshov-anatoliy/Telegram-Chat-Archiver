using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;
using Telegram.HostedApp.Services.Interfaces;

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
				{"ArchiveConfig:OutputPath", "test_archives"},
				{"ArchiveConfig:SyncStatePath", "test_sync.json"},
				{"ArchiveConfig:DatabasePath", "test_stats.db"},
				{"BotConfig:Token", "test_token"},
				{"BotConfig:AdminChatId", "123456789"}
			})
			.Build();

		var services = new ServiceCollection();
		services.Configure<TelegramConfig>(configuration.GetSection("TelegramConfig"));
		services.Configure<ArchiveConfig>(configuration.GetSection("ArchiveConfig"));
		services.Configure<BotConfig>(configuration.GetSection("BotConfig"));
		services.AddSingleton<IMarkdownService, MarkdownService>();
		services.AddSingleton<IMediaDownloadService, MediaDownloadService>();
		services.AddSingleton<ITelegramBotService, TelegramBotService>();
		services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
		services.AddSingleton<ISyncStateService, SyncStateService>();
		services.AddSingleton<IStatisticsService, StatisticsService>();
		services.AddSingleton<IRetryService, RetryService>();
		services.AddLogging();

		// Act
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var telegramConfig = serviceProvider.GetService<IOptions<TelegramConfig>>();
		var archiveConfig = serviceProvider.GetService<IOptions<ArchiveConfig>>();
		var botConfig = serviceProvider.GetService<IOptions<BotConfig>>();
		var markdownService = serviceProvider.GetService<IMarkdownService>();
		var mediaService = serviceProvider.GetService<IMediaDownloadService>();
		var botService = serviceProvider.GetService<ITelegramBotService>();
		var notificationService = serviceProvider.GetService<ITelegramNotificationService>();
		var syncStateService = serviceProvider.GetService<ISyncStateService>();
		var statisticsService = serviceProvider.GetService<IStatisticsService>();
		var retryService = serviceProvider.GetService<IRetryService>();

		Assert.IsNotNull(telegramConfig);
		Assert.IsNotNull(archiveConfig);
		Assert.IsNotNull(botConfig);
		Assert.IsNotNull(markdownService);
		Assert.IsNotNull(mediaService);
		Assert.IsNotNull(botService);
		Assert.IsNotNull(notificationService);
		Assert.IsNotNull(syncStateService);
		Assert.IsNotNull(statisticsService);
		Assert.IsNotNull(retryService);
		Assert.AreEqual(12345, telegramConfig.Value.ApiId);
		Assert.AreEqual("test_hash", telegramConfig.Value.ApiHash);
	}
}
