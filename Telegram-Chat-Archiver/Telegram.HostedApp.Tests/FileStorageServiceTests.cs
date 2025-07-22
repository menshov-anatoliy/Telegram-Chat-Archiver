using Microsoft.Extensions.Configuration;
using Moq;
using Telegram.HostedApp;

namespace Telegram.HostedApp.Tests
{
    [TestClass]
    public class FileStorageServiceTests
    {
        private Mock<IConfiguration> _configurationMock;
        private FileStorageService _fileStorageService;

        [TestInitialize]
        public void TestInitialize()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Storage:WorkingDirectory"]).Returns(Path.GetTempPath());
            _configurationMock.Setup(c => c["Storage:AttachmentsFolder"]).Returns("attachments");
            _configurationMock.Setup(c => c["Formatting:FileNameMask"]).Returns("yyyy-MM-dd'.md'");

            _fileStorageService = new FileStorageService(_configurationMock.Object);
        }

        [TestMethod]
        public async Task GetTodaysNotesFilePathAsync_ShouldReturnCorrectPath()
        {
            // Arrange
            var expectedFileName = DateTime.Now.ToString("yyyy-MM-dd'.md'");
            var expectedPath = Path.Combine(Path.GetTempPath(), expectedFileName);

            // Act
            var actualPath = await _fileStorageService.GetTodaysNotesFilePathAsync();

            // Assert
            Assert.AreEqual(expectedPath, actualPath);
        }

        [TestMethod]
        public async Task AppendTextToNotesAsync_ShouldCreateFileAndAppendText()
        {
            // Arrange
            var textToAppend = "Hello, World!";
            var filePath = await _fileStorageService.GetTodaysNotesFilePathAsync();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Act
            await _fileStorageService.AppendTextToNotesAsync(textToAppend);

            // Assert
            Assert.IsTrue(File.Exists(filePath));
            var fileContent = await File.ReadAllTextAsync(filePath);
            Assert.IsTrue(fileContent.Contains(textToAppend));
        }

        [TestMethod]
        public async Task SaveAttachmentAsync_ShouldCreateFileAndSaveContent()
        {
            // Arrange
            var fileName = "test.txt";
            var fileContent = new byte[] { 1, 2, 3 };
            var attachmentsPath = Path.Combine(Path.GetTempPath(), "attachments");
            var expectedFilePath = Path.Combine(attachmentsPath, fileName);
            if (File.Exists(expectedFilePath))
            {
                File.Delete(expectedFilePath);
            }

            // Act
            var actualFilePath = await _fileStorageService.SaveAttachmentAsync(fileContent, fileName);

            // Assert
            Assert.AreEqual(expectedFilePath, actualFilePath);
            Assert.IsTrue(File.Exists(actualFilePath));
            var savedContent = await File.ReadAllBytesAsync(actualFilePath);
            CollectionAssert.AreEqual(fileContent, savedContent);
        }
    }
}
