using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Telegram.Bot.Exceptions;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.Tests;

[TestClass]
public class RetryServiceTests
{
    private Mock<ILogger<RetryService>>? _mockLogger;
    private Mock<IOptions<ArchiveConfig>>? _mockOptions;
    private ArchiveConfig? _config;
    private RetryService? _service;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<RetryService>>();
        _mockOptions = new Mock<IOptions<ArchiveConfig>>();
        _config = new ArchiveConfig
        {
            MaxRetryAttempts = 3,
            BaseRetryDelayMs = 100
        };
        _mockOptions.Setup(x => x.Value).Returns(_config);
        _service = new RetryService(_mockLogger.Object, _mockOptions.Object);
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = "success";
        Func<Task<string>> operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _service!.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_TransientError_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        var expectedResult = "success";
        
        Func<Task<string>> operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new HttpRequestException("Temporary network error");
            }
            return Task.FromResult(expectedResult);
        };

        // Act
        var result = await _service!.ExecuteWithRetryAsync(operation, 3, 10);

        // Assert
        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(2, callCount);
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_NonTransientError_ThrowsImmediately()
    {
        // Arrange
        var callCount = 0;
        var expectedException = new ArgumentException("Non-transient error");
        
        Func<Task<string>> operation = () =>
        {
            callCount++;
            throw expectedException;
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service!.ExecuteWithRetryAsync(operation, 3, 10));
        
        Assert.AreEqual(expectedException.Message, exception.Message);
        Assert.AreEqual(1, callCount); // Should not retry
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_ExceedsMaxRetries_ThrowsLastException()
    {
        // Arrange
        var callCount = 0;
        var expectedException = new HttpRequestException("Persistent network error");
        
        Func<Task<string>> operation = () =>
        {
            callCount++;
            throw expectedException;
        };

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => _service!.ExecuteWithRetryAsync(operation, 2, 10));
        
        Assert.AreEqual(expectedException.Message, exception.Message);
        Assert.AreEqual(2, callCount); // Should retry exactly maxAttempts times
    }

    [TestMethod]
    public void IsTransientException_TransientExceptions_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(_service!.IsTransientException(new HttpRequestException()));
        Assert.IsTrue(_service.IsTransientException(new TaskCanceledException()));
        Assert.IsTrue(_service.IsTransientException(new TimeoutException()));
        Assert.IsTrue(_service.IsTransientException(new IOException()));
        Assert.IsTrue(_service.IsTransientException(new UnauthorizedAccessException()));
        Assert.IsTrue(_service.IsTransientException(new ApiRequestException("Rate limit", 429)));
        Assert.IsTrue(_service.IsTransientException(new ApiRequestException("Server error", 500)));
        Assert.IsTrue(_service.IsTransientException(new ApiRequestException("Bad gateway", 502)));
        Assert.IsTrue(_service.IsTransientException(new ApiRequestException("Service unavailable", 503)));
        Assert.IsTrue(_service.IsTransientException(new ApiRequestException("Gateway timeout", 504)));
    }

    [TestMethod]
    public void IsTransientException_NonTransientExceptions_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.IsFalse(_service!.IsTransientException(new ArgumentException()));
        Assert.IsFalse(_service.IsTransientException(new InvalidOperationException()));
        Assert.IsFalse(_service.IsTransientException(new ApiRequestException("Bad request", 400)));
        Assert.IsFalse(_service.IsTransientException(new ApiRequestException("Unauthorized", 401)));
        Assert.IsFalse(_service.IsTransientException(new ApiRequestException("Forbidden", 403)));
        Assert.IsFalse(_service.IsTransientException(new ApiRequestException("Not found", 404)));
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_VoidOperation_ExecutesSuccessfully()
    {
        // Arrange
        var callCount = 0;
        Func<Task> operation = () =>
        {
            callCount++;
            return Task.CompletedTask;
        };

        // Act
        await _service!.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task ExecuteWithRetryAsync_VoidOperationWithRetries_RetriesCorrectly()
    {
        // Arrange
        var callCount = 0;
        Func<Task> operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new HttpRequestException("Temporary error");
            }
            return Task.CompletedTask;
        };

        // Act
        await _service!.ExecuteWithRetryAsync(operation);

        // Assert
        Assert.AreEqual(2, callCount);
    }
}