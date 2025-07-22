using Microsoft.Extensions.Configuration;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace Telegram.HostedApp.Tests
{
    [TestClass]
    public class ArchiverServiceTests
    {
        private Mock<IConfiguration> _configurationMock;
        private Mock<ITelegramBotClient> _botClientMock;
        private Mock<IFileStorageService> _fileStorageServiceMock;
        private ArchiverService _archiverService;

        [TestInitialize]
        public void TestInitialize()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Telegram:ChatId"]).Returns("12345");
            _configurationMock.Setup(c => c["Formatting:HeaderMask"]).Returns("yyyy-MM-dd HH:mm:ss");

            _botClientMock = new Mock<ITelegramBotClient>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();

            _archiverService = new ArchiverService(_botClientMock.Object, _configurationMock.Object, _fileStorageServiceMock.Object, null);
        }

        [TestMethod]
        public async Task HandleUpdateAsync_ShouldProcessTextMessage()
        {
            // Arrange
            var message = new Message
            {
                MessageId = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 12345 },
                Text = "Hello, World!"
            };
            var update = new Update { Message = message };

            // Act
            await (Task)_archiverService.GetType().GetMethod("HandleUpdateAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_archiverService, new object[] { _botClientMock.Object, update, CancellationToken.None });

            // Assert
            _fileStorageServiceMock.Verify(s => s.AppendTextToNotesAsync(It.Is<string>(str => str.Contains("Hello, World!"))), Times.Once);
        }
    }
}
