using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests;

[TestClass]
public sealed class MarkdownServiceTests
{
    private MarkdownService _markdownService = null!;
    private string _testOutputPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _testOutputPath = Path.Combine(Path.GetTempPath(), "telegram_archiver_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputPath);

        var config = new ArchiveConfig 
        { 
            OutputPath = _testOutputPath,
            MediaPath = Path.Combine(_testOutputPath, "media")
        };
        
        var logger = new LoggerFactory().CreateLogger<MarkdownService>();
        var options = Options.Create(config);
        
        _markdownService = new MarkdownService(logger, options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }
    }

    [TestMethod]
    public void GetMarkdownFilePath_ShouldReturnCorrectPath()
    {
        // Arrange
        var chatTitle = "Тестовый чат";
        var date = new DateTime(2024, 1, 15);

        // Act
        var filePath = _markdownService.GetMarkdownFilePath(chatTitle, date);

        // Assert
        var expectedPath = Path.Combine(_testOutputPath, "Тестовый_чат", "2024-01-15.md");
        Assert.AreEqual(expectedPath, filePath);
    }

    [TestMethod]
    public async Task SaveMessagesAsync_ShouldCreateMarkdownFile()
    {
        // Arrange
        var chatTitle = "Тестовый чат";
        var date = new DateTime(2024, 1, 15);
        var messages = new[]
        {
            new ChatMessage
            {
                Id = 1,
                Date = date.AddHours(10),
                AuthorName = "Тестовый пользователь",
                AuthorId = 12345,
                Text = "Привет, мир!",
                Type = MessageType.Text
            }
        };

        // Act
        await _markdownService.SaveMessagesAsync(messages, chatTitle, date);

        // Assert
        var filePath = _markdownService.GetMarkdownFilePath(chatTitle, date);
        Assert.IsTrue(File.Exists(filePath), $"Файл не найден: {filePath}");
        
        var content = await File.ReadAllTextAsync(filePath);
        
        Assert.IsTrue(content.Contains("# Тестовый чат"), "Заголовок чата не найден");
        Assert.IsTrue(content.Contains("Привет, мир\\!"), "Текст сообщения не найден"); // Экранированный восклицательный знак
        Assert.IsTrue(content.Contains("Тестовый пользователь"), "Имя пользователя не найдено");
    }

    [TestMethod]
    public async Task AppendMessageAsync_ShouldAddMessageToFile()
    {
        // Arrange
        var chatTitle = "Тестовый чат";
        var message = new ChatMessage
        {
            Id = 2,
            Date = new DateTime(2024, 1, 15, 14, 30, 0),
            AuthorName = "Второй пользователь",
            AuthorId = 67890,
            Text = "Второе сообщение",
            Type = MessageType.Text
        };

        // Act
        await _markdownService.AppendMessageAsync(message, chatTitle);

        // Assert
        var filePath = _markdownService.GetMarkdownFilePath(chatTitle, message.Date);
        Assert.IsTrue(File.Exists(filePath));
        
        var content = await File.ReadAllTextAsync(filePath);
        Assert.IsTrue(content.Contains("Второе сообщение"));
        Assert.IsTrue(content.Contains("Второй пользователь"));
    }
}

[TestClass]
public sealed class MediaDownloadServiceTests
{
    private MediaDownloadService _mediaDownloadService = null!;
    private string _testMediaPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _testMediaPath = Path.Combine(Path.GetTempPath(), "telegram_archiver_media_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testMediaPath);

        var config = new ArchiveConfig 
        { 
            MediaPath = _testMediaPath 
        };
        
        var logger = new LoggerFactory().CreateLogger<MediaDownloadService>();
        var options = Options.Create(config);
        
        _mediaDownloadService = new MediaDownloadService(logger, options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testMediaPath))
        {
            Directory.Delete(_testMediaPath, true);
        }
    }

    [TestMethod]
    public void GetMediaPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var chatTitle = "Тестовый чат";
        var messageDate = new DateTime(2024, 1, 15);
        var fileName = "test_image.jpg";

        // Act
        var mediaPath = _mediaDownloadService.GetMediaPath(chatTitle, messageDate, fileName);

        // Assert
        var expectedPath = Path.Combine(_testMediaPath, "Тестовый_чат", "2024-01", "test_image.jpg");
        Assert.AreEqual(expectedPath, mediaPath);
    }

    [TestMethod]
    public async Task DownloadMediaAsync_ShouldCreatePlaceholderFile()
    {
        // Arrange
        var chatTitle = "Тестовый чат";
        var messageDate = new DateTime(2024, 1, 15);
        var media = new MediaInfo
        {
            FileName = "test_document.pdf",
            FileSize = 1024000,
            MimeType = "application/pdf"
        };

        // Act
        var result = await _mediaDownloadService.DownloadMediaAsync(media, chatTitle, messageDate);

        // Assert
        Assert.IsNotNull(result.LocalPath);
        Assert.IsTrue(File.Exists(result.LocalPath));
        
        var content = await File.ReadAllTextAsync(result.LocalPath);
        Assert.IsTrue(content.Contains("test_document.pdf"));
        Assert.IsTrue(content.Contains("1024000"));
    }
}