using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests;

[TestClass]
public class SyncStateServiceTests
{
    private Mock<ILogger<SyncStateService>>? _mockLogger;
    private Mock<IOptions<ArchiveConfig>>? _mockOptions;
    private ArchiveConfig? _config;
    private SyncStateService? _service;
    private string? _testFilePath;

    [TestInitialize]
    public void Setup()
    {
        // Создаем уникальное имя файла для каждого теста
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_sync_state_{Guid.NewGuid():N}.json");
        
        _mockLogger = new Mock<ILogger<SyncStateService>>();
        _mockOptions = new Mock<IOptions<ArchiveConfig>>();
        _config = new ArchiveConfig
        {
            SyncStatePath = _testFilePath
        };
        _mockOptions.Setup(x => x.Value).Returns(_config);
        _service = new SyncStateService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestMethod]
    public async Task LoadSyncStateAsync_NewChat_ReturnsNewSyncState()
    {
        // Arrange
        var chatId = 12345L;

        // Act
        var result = await _service!.LoadSyncStateAsync(chatId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(chatId, result.ChatId);
        Assert.AreEqual(0, result.LastProcessedMessageId);
        Assert.AreEqual(SyncStatus.Idle, result.Status);
        Assert.AreEqual(0, result.TotalProcessedMessages);
    }

    [TestMethod]
    public async Task UpdateLastProcessedMessageAsync_IncreasesMessageId_UpdatesState()
    {
        // Arrange
        var chatId = 12345L;
        var messageId = 100;

        // Act
        await _service!.UpdateLastProcessedMessageAsync(chatId, messageId);
        var result = await _service.LoadSyncStateAsync(chatId);

        // Assert
        Assert.AreEqual(messageId, result.LastProcessedMessageId);
        Assert.AreEqual(1, result.TotalProcessedMessages);
    }

    [TestMethod]
    public async Task SetSyncStatusAsync_ChangesStatus_UpdatesState()
    {
        // Arrange
        var chatId = 12345L;
        var status = SyncStatus.InProgress;
        var errorMessage = "Test error";

        // Act
        await _service!.SetSyncStatusAsync(chatId, status, errorMessage);
        var result = await _service.LoadSyncStateAsync(chatId);

        // Assert
        Assert.AreEqual(status, result.Status);
        Assert.AreEqual(errorMessage, result.ErrorMessage);
    }

    [TestMethod]
    public async Task ResetSyncStateAsync_ResetsState_CreatesNewState()
    {
        // Arrange
        var chatId = 12345L;

        // Предварительно установим некоторое состояние
        await _service!.UpdateLastProcessedMessageAsync(chatId, 100);

        // Act
        await _service.ResetSyncStateAsync(chatId);
        var result = await _service.LoadSyncStateAsync(chatId);

        // Assert
        Assert.AreEqual(0, result.LastProcessedMessageId);
        Assert.AreNotEqual(default(DateTime), result.LastFullSyncDate);
        Assert.AreEqual(0, result.TotalProcessedMessages);
        Assert.AreEqual(SyncStatus.Idle, result.Status);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            // Освобождаем ресурсы сервиса
            _service?.Dispose();
        }
        catch { }

        try
        {
            // Удаляем конкретный тестовый файл
            if (!string.IsNullOrEmpty(_testFilePath) && File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }
        catch
        {
            // Игнорируем ошибки удаления
        }

        try
        {
            // Дополнительная очистка всех sync_state файлов в temp папке (на всякий случай)
            var tempPath = Path.GetTempPath();
            var syncFiles = Directory.GetFiles(tempPath, "*sync_state*.json");
            foreach (var file in syncFiles)
            {
                try
                {
                    if (DateTime.Now - File.GetCreationTime(file) > TimeSpan.FromHours(1))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Игнорируем ошибки удаления старых файлов
                }
            }
        }
        catch
        {
            // Игнорируем ошибки при очистке
        }
    }
}