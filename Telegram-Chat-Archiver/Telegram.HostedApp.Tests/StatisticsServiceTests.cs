using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests;

[TestClass]
public class StatisticsServiceTests
{
    private Mock<ILogger<StatisticsService>> _mockLogger;
    private Mock<IOptions<ArchiveConfig>> _mockOptions;
    private ArchiveConfig _config;
    private StatisticsService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<StatisticsService>>();
        _mockOptions = new Mock<IOptions<ArchiveConfig>>();
        _config = new ArchiveConfig();
        _mockOptions.Setup(x => x.Value).Returns(_config);
        _service = new StatisticsService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestMethod]
    public async Task RecordMessageProcessedAsync_UpdatesStatistics()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = 1,
            AuthorId = 12345,
            AuthorName = "Test User",
            Type = MessageType.Text,
            Date = DateTime.UtcNow
        };
        var processingTime = 150.0;

        // Act
        await _service.RecordMessageProcessedAsync(message, processingTime);
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.AreEqual(1, stats.TotalMessagesProcessed);
        Assert.AreEqual(processingTime, stats.AverageProcessingTimeMs);
        Assert.IsTrue(stats.MessageTypeStats.ContainsKey(MessageType.Text));
        Assert.AreEqual(1, stats.MessageTypeStats[MessageType.Text]);
        Assert.IsTrue(stats.AuthorStats.ContainsKey(12345));
        Assert.AreEqual(1, stats.AuthorStats[12345].MessageCount);
    }

    [TestMethod]
    public async Task RecordMediaDownloadAsync_UpdatesStatistics()
    {
        // Arrange
        var fileSize = 1024L;

        // Act
        await _service.RecordMediaDownloadAsync(fileSize);
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.AreEqual(1, stats.MediaFilesDownloaded);
        Assert.AreEqual(fileSize, stats.TotalDownloadedSizeBytes);
    }

    [TestMethod]
    public async Task RecordErrorAsync_UpdatesErrorCount()
    {
        // Arrange
        var error = "Test error";
        var exception = new Exception("Test exception");

        // Act
        await _service.RecordErrorAsync(error, exception);
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.AreEqual(1, stats.ErrorCount);
    }

    [TestMethod]
    public async Task RecordArchiveCreatedAsync_UpdatesArchiveCount()
    {
        // Act
        await _service.RecordArchiveCreatedAsync();
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.AreEqual(1, stats.ArchiveFilesCreated);
        Assert.IsNotNull(stats.LastArchiveTime);
    }

    [TestMethod]
    public async Task ResetSessionStatisticsAsync_ResetsCounters()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = 1,
            AuthorId = 12345,
            Type = MessageType.Text,
            Date = DateTime.UtcNow
        };

        await _service.RecordMessageProcessedAsync(message, 100.0);
        await _service.RecordMediaDownloadAsync(1024);
        await _service.RecordErrorAsync("test error");

        // Act
        await _service.ResetSessionStatisticsAsync();
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.AreEqual(0, stats.TotalMessagesProcessed);
        Assert.AreEqual(0, stats.MediaFilesDownloaded);
        Assert.AreEqual(0, stats.ErrorCount);
        Assert.AreEqual(0, stats.MessageTypeStats.Count);
        Assert.AreEqual(0, stats.AuthorStats.Count);
    }

    [TestMethod]
    public async Task GetAuthorStatisticsAsync_ReturnsCorrectData()
    {
        // Arrange
        var message1 = new ChatMessage
        {
            Id = 1,
            AuthorId = 12345,
            AuthorName = "User 1",
            Type = MessageType.Text,
            Date = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            Id = 2,
            AuthorId = 12345,  // Same author
            AuthorName = "User 1",
            Type = MessageType.Photo,
            Date = DateTime.UtcNow.AddMinutes(1),
            Media = new MediaInfo { FileName = "test.jpg" }
        };

        var message3 = new ChatMessage
        {
            Id = 3,
            AuthorId = 67890,  // Different author
            AuthorName = "User 2",
            Type = MessageType.Text,
            Date = DateTime.UtcNow.AddMinutes(2)
        };

        // Act
        await _service.RecordMessageProcessedAsync(message1, 100.0);
        await _service.RecordMessageProcessedAsync(message2, 100.0);
        await _service.RecordMessageProcessedAsync(message3, 100.0);

        var authorStats = await _service.GetAuthorStatisticsAsync();

        // Assert
        Assert.AreEqual(2, authorStats.Count);
        
        Assert.IsTrue(authorStats.ContainsKey(12345));
        Assert.AreEqual(2, authorStats[12345].MessageCount);
        Assert.AreEqual(1, authorStats[12345].MediaCount); // Only message2 has media
        Assert.AreEqual("User 1", authorStats[12345].AuthorName);

        Assert.IsTrue(authorStats.ContainsKey(67890));
        Assert.AreEqual(1, authorStats[67890].MessageCount);
        Assert.AreEqual(0, authorStats[67890].MediaCount);
        Assert.AreEqual("User 2", authorStats[67890].AuthorName);
    }

    [TestMethod]
    public async Task GetMessageTypeStatisticsAsync_ReturnsCorrectData()
    {
        // Arrange
        var messages = new[]
        {
            new ChatMessage { Id = 1, AuthorId = 1, Type = MessageType.Text, Date = DateTime.UtcNow },
            new ChatMessage { Id = 2, AuthorId = 1, Type = MessageType.Text, Date = DateTime.UtcNow },
            new ChatMessage { Id = 3, AuthorId = 1, Type = MessageType.Photo, Date = DateTime.UtcNow },
            new ChatMessage { Id = 4, AuthorId = 1, Type = MessageType.Video, Date = DateTime.UtcNow }
        };

        // Act
        foreach (var message in messages)
        {
            await _service.RecordMessageProcessedAsync(message, 100.0);
        }

        var typeStats = await _service.GetMessageTypeStatisticsAsync();

        // Assert
        Assert.AreEqual(3, typeStats.Count);
        Assert.AreEqual(2, typeStats[MessageType.Text]);
        Assert.AreEqual(1, typeStats[MessageType.Photo]);
        Assert.AreEqual(1, typeStats[MessageType.Video]);
    }
}