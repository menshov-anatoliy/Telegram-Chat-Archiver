using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests.Performance;

/// <summary>
/// Тесты производительности для основных сервисов
/// </summary>
[TestClass]
public class PerformanceTests
{
    private Mock<ILogger<MarkdownService>> _mockLogger;
    private Mock<IOptions<ArchiveConfig>> _mockOptions;
    private ArchiveConfig _config;
    private MarkdownService _markdownService;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MarkdownService>>();
        _mockOptions = new Mock<IOptions<ArchiveConfig>>();
        _config = new ArchiveConfig
        {
            OutputPath = Path.GetTempPath(),
            FileNameFormat = "{Date:yyyy-MM-dd}.md",
            MaxMessagesPerFile = 1000
        };
        _mockOptions.Setup(x => x.Value).Returns(_config);
        _markdownService = new MarkdownService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestMethod]
    public async Task MarkdownGeneration_Performance_ShouldProcessLargeVolumeEfficiently()
    {
        // Arrange
        const int messageCount = 10000;
        var messages = GenerateTestMessages(messageCount);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var markdownPath = await _markdownService.SaveMessagesAsync(messages, DateTime.Now);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(File.Exists(markdownPath), "Markdown файл должен быть создан");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Обработка {messageCount} сообщений должна занимать менее 5 секунд. Фактическое время: {stopwatch.ElapsedMilliseconds}ms");
        
        // Проверяем размер файла
        var fileInfo = new FileInfo(markdownPath);
        Assert.IsTrue(fileInfo.Length > 0, "Файл должен содержать данные");
        
        // Очистка
        File.Delete(markdownPath);
    }

    [TestMethod]
    public async Task MarkdownGeneration_MemoryUsage_ShouldNotExceedThreshold()
    {
        // Arrange
        const int messageCount = 5000;
        var messages = GenerateTestMessages(messageCount);
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var markdownPath = await _markdownService.SaveMessagesAsync(messages, DateTime.Now);
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerMessage = memoryIncrease / messageCount;
        
        Assert.IsTrue(memoryIncreasePerMessage < 1024, // 1KB per message threshold
            $"Использование памяти на сообщение превышает порог: {memoryIncreasePerMessage} bytes");
        
        // Очистка
        File.Delete(markdownPath);
        GC.Collect();
    }

    [TestMethod]
    public async Task MarkdownGeneration_ConcurrentAccess_ShouldHandleMultipleThreads()
    {
        // Arrange
        const int threadCount = 10;
        const int messagesPerThread = 1000;
        var tasks = new List<Task<string>>();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < threadCount; i++)
        {
            var messages = GenerateTestMessages(messagesPerThread);
            var task = _markdownService.SaveMessagesAsync(messages, DateTime.Now.AddHours(i));
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(threadCount, results.Length, "Все потоки должны завершиться успешно");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000, 
            $"Параллельная обработка должна занимать менее 10 секунд. Фактическое время: {stopwatch.ElapsedMilliseconds}ms");

        foreach (var result in results)
        {
            Assert.IsTrue(File.Exists(result), $"Файл {result} должен существовать");
            File.Delete(result);
        }
    }

    [TestMethod]
    public async Task AppendMessage_Performance_ShouldHandleHighThroughput()
    {
        // Arrange
        const int messageCount = 50000;
        var testFile = Path.GetTempFileName();
        var stopwatch = new Stopwatch();

        try
        {
            // Act
            stopwatch.Start();
            for (int i = 0; i < messageCount; i++)
            {
                var message = new Models.ChatMessage
                {
                    Id = i,
                    Text = $"Test message {i}",
                    Date = DateTime.Now.AddMinutes(-i),
                    AuthorName = $"User{i % 100}"
                };
                await _markdownService.AppendMessageAsync(testFile, message);
            }
            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000, 
                $"Добавление {messageCount} сообщений должно занимать менее 30 секунд. Фактическое время: {stopwatch.ElapsedMilliseconds}ms");

            var throughput = messageCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            Assert.IsTrue(throughput > 1000, 
                $"Пропускная способность должна быть больше 1000 сообщений/сек. Фактическая: {throughput:F2}");

            // Проверяем что файл содержит все сообщения
            var fileContent = await File.ReadAllTextAsync(testFile);
            var lineCount = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.IsTrue(lineCount >= messageCount, 
                $"Файл должен содержать минимум {messageCount} строк. Фактически: {lineCount}");
        }
        finally
        {
            // Очистка
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [TestMethod]
    [DataRow(1000)]
    [DataRow(5000)]
    [DataRow(10000)]
    public async Task MarkdownGeneration_Scalability_ShouldScaleLinearlyWithMessageCount(int messageCount)
    {
        // Arrange
        var messages = GenerateTestMessages(messageCount);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var markdownPath = await _markdownService.SaveMessagesAsync(messages, DateTime.Now);
        stopwatch.Stop();

        // Assert
        var timePerMessage = (double)stopwatch.ElapsedMilliseconds / messageCount;
        Assert.IsTrue(timePerMessage < 0.5, // 0.5ms per message threshold
            $"Время обработки на сообщение превышает порог: {timePerMessage:F3}ms для {messageCount} сообщений");

        // Очистка
        File.Delete(markdownPath);
    }

    [TestMethod]
    public async Task LargeMessage_Performance_ShouldHandleLargeContentEfficiently()
    {
        // Arrange
        var largeContent = new string('A', 100000); // 100KB message
        var message = new Models.ChatMessage
        {
            Id = 1,
            Text = largeContent,
            Date = DateTime.Now,
            AuthorName = "TestUser"
        };
        var messages = new[] { message };
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var markdownPath = await _markdownService.SaveMessagesAsync(messages, DateTime.Now);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
            $"Обработка большого сообщения должна занимать менее 1 секунды. Фактическое время: {stopwatch.ElapsedMilliseconds}ms");

        var fileInfo = new FileInfo(markdownPath);
        Assert.IsTrue(fileInfo.Length > largeContent.Length,
            "Размер файла должен быть больше размера исходного сообщения (из-за форматирования)");

        // Очистка
        File.Delete(markdownPath);
    }

    /// <summary>
    /// Генерация тестовых сообщений для нагрузочных тестов
    /// </summary>
    private Models.ChatMessage[] GenerateTestMessages(int count)
    {
        var messages = new Models.ChatMessage[count];
        var random = new Random(42); // Fixed seed for reproducible tests
        var messageTypes = new[] { "text", "photo", "video", "document", "voice" };
        var authors = Enumerable.Range(1, 50).Select(i => $"User{i}").ToArray();

        for (int i = 0; i < count; i++)
        {
            messages[i] = new Models.ChatMessage
            {
                Id = i + 1,
                Text = $"Test message {i + 1} with some content to make it realistic. Random number: {random.Next(1000)}",
                Date = DateTime.Now.AddMinutes(-i),
                AuthorName = authors[random.Next(authors.Length)],
                Type = messageTypes[random.Next(messageTypes.Length)],
                MediaPath = random.Next(10) < 3 ? $"media/file_{i}.jpg" : null // 30% chance of media
            };
        }

        return messages;
    }
}

/// <summary>
/// Нагрузочные тесты для проверки производительности под высокой нагрузкой
/// </summary>
[TestClass]
public class LoadTests
{
    private Mock<ILogger<StatisticsService>> _mockLogger;
    private Mock<IOptions<ArchiveConfig>> _mockOptions;
    private ArchiveConfig _config;
    private StatisticsService _statisticsService;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<StatisticsService>>();
        _mockOptions = new Mock<IOptions<ArchiveConfig>>();
        _config = new ArchiveConfig
        {
            DatabasePath = Path.GetTempFileName()
        };
        _mockOptions.Setup(x => x.Value).Returns(_config);
        _statisticsService = new StatisticsService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_config.DatabasePath))
            File.Delete(_config.DatabasePath);
    }

    [TestMethod]
    public async Task StatisticsService_HighConcurrency_ShouldHandleMultipleThreadsWriting()
    {
        // Arrange
        const int threadCount = 20;
        const int operationsPerThread = 1000;
        var tasks = new List<Task>();
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        for (int i = 0; i < threadCount; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    await _statisticsService.RecordMessageProcessedAsync($"User{threadId}", "text");
                    if (j % 10 == 0) // Every 10th operation is media download
                    {
                        await _statisticsService.RecordMediaDownloadAsync("photo", 1024 * (j + 1));
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000,
            $"Высоконагруженные операции должны завершиться за 30 секунд. Фактическое время: {stopwatch.ElapsedMilliseconds}ms");

        // Проверяем что статистики корректны
        var messageStats = await _statisticsService.GetMessageTypeStatisticsAsync();
        var totalMessages = messageStats.Values.Sum();
        Assert.AreEqual(threadCount * operationsPerThread, totalMessages,
            "Общее количество обработанных сообщений должно соответствовать ожидаемому");
    }

    [TestMethod]
    public async Task StatisticsService_MemoryLeak_ShouldNotLeakMemoryUnderLoad()
    {
        // Arrange
        const int iterations = 1000;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _statisticsService.RecordMessageProcessedAsync($"User{i % 10}", "text");
            await _statisticsService.RecordMediaDownloadAsync("photo", 1024);
            
            // Каждые 100 итераций проверяем статистики
            if (i % 100 == 0)
            {
                var stats = await _statisticsService.GetMessageTypeStatisticsAsync();
                var authorStats = await _statisticsService.GetAuthorStatisticsAsync();
            }
        }

        // Принудительная сборка мусора
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerOperation = memoryIncrease / iterations;
        
        Assert.IsTrue(memoryIncreasePerOperation < 1024, // 1KB per operation threshold
            $"Утечка памяти обнаружена: {memoryIncreasePerOperation} bytes на операцию");
    }

    [TestMethod]
    public async Task StatisticsService_DatabaseSize_ShouldNotGrowExcessively()
    {
        // Arrange
        const int messageCount = 10000;
        var dbFile = new FileInfo(_config.DatabasePath);
        var initialSize = dbFile.Exists ? dbFile.Length : 0;

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            await _statisticsService.RecordMessageProcessedAsync($"User{i % 100}", "text");
        }

        // Assert
        dbFile.Refresh();
        var finalSize = dbFile.Length;
        var sizeIncrease = finalSize - initialSize;
        var sizePerMessage = sizeIncrease / messageCount;

        Assert.IsTrue(sizePerMessage < 100, // 100 bytes per message threshold
            $"Размер базы данных растет слишком быстро: {sizePerMessage} bytes на сообщение");

        Assert.IsTrue(finalSize < 10 * 1024 * 1024, // 10MB max database size
            $"Размер базы данных превышает допустимый лимит: {finalSize / (1024 * 1024)}MB");
    }
}