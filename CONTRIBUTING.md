# Contributing to Telegram Chat Archiver

Мы рады, что вы хотите внести вклад в развитие Telegram Chat Archiver! Этот документ описывает процесс и правила для contributing.

## 📋 Содержание

- [Code of Conduct](#code-of-conduct)
- [Как я могу помочь?](#как-я-могу-помочь)
- [Процесс разработки](#процесс-разработки)
- [Настройка среды разработки](#настройка-среды-разработки)
- [Стандарты кода](#стандарты-кода)
- [Commit сообщения](#commit-сообщения)
- [Pull Request процесс](#pull-request-процесс)
- [Тестирование](#тестирование)
- [Документация](#документация)

## Code of Conduct

Участвуя в этом проекте, вы соглашаетесь соблюдать наш [Code of Conduct](CODE_OF_CONDUCT.md). Пожалуйста, сообщайте о неприемлемом поведении на [email@example.com].

## Как я могу помочь?

### 🐛 Сообщение об ошибках

Если вы нашли ошибку, создайте issue с:

- **Подробным описанием** проблемы
- **Шагами для воспроизведения**
- **Ожидаемым поведением**
- **Фактическим поведением**
- **Версией** приложения и окружения
- **Логами** (без конфиденциальных данных)

### 💡 Предложение новых функций

Для предложения новой функциональности:

1. Проверьте, нет ли уже похожего предложения в [Issues](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues)
2. Создайте issue с тегом `enhancement`
3. Опишите:
   - **Зачем** нужна эта функция
   - **Как** она должна работать
   - **Примеры использования**

### 📚 Улучшение документации

Документацию всегда можно улучшить:

- Исправление опечаток
- Добавление примеров
- Улучшение объяснений
- Перевод на другие языки

### 💻 Разработка кода

Ищите issues с метками:

- `good first issue` - для начинающих
- `help wanted` - нужна помощь
- `bug` - исправление ошибок
- `enhancement` - новые функции

## Процесс разработки

### 1. Fork и Clone

```bash
# Fork репозиторий через GitHub UI
# Затем клонируйте ваш fork
git clone https://github.com/your-username/Telegram-Chat-Archiver.git
cd Telegram-Chat-Archiver

# Добавьте upstream remote
git remote add upstream https://github.com/menshov-anatoliy/Telegram-Chat-Archiver.git
```

### 2. Создание ветки

```bash
# Обновите main ветку
git checkout main
git pull upstream main

# Создайте новую ветку
git checkout -b feature/your-feature-name
# или
git checkout -b fix/your-bug-fix
```

### 3. Разработка

Внесите ваши изменения, следуя [стандартам кода](#стандарты-кода).

### 4. Commit и Push

```bash
# Добавьте изменения
git add .

# Сделайте commit с осмысленным сообщением
git commit -m "feat: add amazing new feature"

# Push в ваш fork
git push origin feature/your-feature-name
```

### 5. Pull Request

Создайте Pull Request через GitHub UI.

## Настройка среды разработки

### Требования

- **.NET 8.0 SDK**
- **Git**
- **Docker** (опционально)
- **IDE**: Visual Studio 2022, VS Code, или JetBrains Rider

### Установка

```bash
# Клонирование
git clone https://github.com/your-username/Telegram-Chat-Archiver.git
cd Telegram-Chat-Archiver

# Восстановление пакетов
dotnet restore

# Сборка
dotnet build

# Запуск тестов
dotnet test
```

### Настройка конфигурации

```bash
# Скопируйте пример конфигурации
cp .env.example .env

# Отредактируйте .env с вашими тестовыми данными
# НЕ используйте production данные для разработки!
```

### Запуск в режиме разработки

```bash
cd Telegram.HostedApp
dotnet run --environment Development
```

## Стандарты кода

### Общие принципы

- **SOLID принципы**
- **Clean Code** практики
- **DRY** (Don't Repeat Yourself)
- **YAGNI** (You Aren't Gonna Need It)

### C# Code Style

```csharp
// ✅ Хорошо
public class TelegramArchiverService : ITelegramArchiverService
{
    private readonly ILogger<TelegramArchiverService> _logger;
    private readonly IConfiguration _configuration;

    public TelegramArchiverService(
        ILogger<TelegramArchiverService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Архивирует сообщения из указанного чата
    /// </summary>
    /// <param name="chatId">Идентификатор чата</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество архивированных сообщений</returns>
    public async Task<int> ArchiveMessagesAsync(
        string chatId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Начинаю архивирование чата {ChatId}", chatId);
        
        try
        {
            // Логика архивирования
            return messageCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при архивировании чата {ChatId}", chatId);
            throw;
        }
    }
}

// ❌ Плохо
public class bad_service
{
    public void DoSomething()
    {
        // Нет логирования
        // Нет обработки ошибок
        // Неясное название
    }
}
```

### Naming Conventions

- **PascalCase**: классы, методы, свойства, enum значения
- **camelCase**: локальные переменные, параметры, приватные поля
- **_camelCase**: приватные поля с подчеркиванием
- **UPPER_CASE**: константы

```csharp
// ✅ Правильно
public class MessageProcessor
{
    private const int MAX_RETRY_ATTEMPTS = 3;
    private readonly ILogger<MessageProcessor> _logger;
    
    public async Task<ProcessResult> ProcessMessageAsync(ChatMessage message)
    {
        var processedCount = 0;
        // ...
    }
}
```

### Комментарии

- **Комментарии на русском языке**
- **XML документация** для публичных методов
- **Комментарии объясняют "почему", не "что"**

```csharp
/// <summary>
/// Обрабатывает сообщения пакетно для повышения производительности
/// </summary>
/// <param name="messages">Сообщения для обработки</param>
/// <param name="batchSize">Размер пакета (по умолчанию 100)</param>
/// <returns>Результат обработки пакета</returns>
public async Task<BatchProcessResult> ProcessBatchAsync(
    IEnumerable<ChatMessage> messages, 
    int batchSize = 100)
{
    // Пакетная обработка снижает нагрузку на API
    // и улучшает производительность в 3-5 раз
    foreach (var batch in messages.Batch(batchSize))
    {
        // ...
    }
}
```

### Обработка ошибок

```csharp
// ✅ Правильно
public async Task<Result<string>> SaveFileAsync(string content, string path)
{
    try
    {
        await File.WriteAllTextAsync(path, content);
        _logger.LogInformation("Файл сохранен: {Path}", path);
        return Result.Success(path);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Нет прав доступа для записи в {Path}", path);
        return Result.Failure<string>("Недостаточно прав для записи файла");
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "Ошибка ввода-вывода при записи {Path}", path);
        return Result.Failure<string>("Ошибка записи файла");
    }
}
```

## Commit сообщения

Используем [Conventional Commits](https://www.conventionalcommits.org/):

### Формат

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Типы

- **feat**: новая функциональность
- **fix**: исправление ошибки
- **docs**: изменения в документации
- **style**: форматирование кода (без изменения логики)
- **refactor**: рефакторинг кода
- **perf**: улучшение производительности
- **test**: добавление или изменение тестов
- **chore**: изменения в build процессе или auxiliary tools

### Примеры

```bash
# Новая функция
git commit -m "feat(archiver): add support for voice messages"

# Исправление ошибки
git commit -m "fix(api): handle null response from telegram api"

# Документация
git commit -m "docs: update README with docker instructions"

# Рефакторинг
git commit -m "refactor(services): extract message processing logic"

# Критическое изменение
git commit -m "feat!: change configuration format

BREAKING CHANGE: configuration file format changed from JSON to YAML"
```

## Pull Request процесс

### Подготовка PR

1. **Убедитесь, что ваш код соответствует стандартам**
2. **Все тесты проходят**
3. **Документация обновлена** (если нужно)
4. **Нет merge конфликтов**

### Шаблон PR

```markdown
## Описание

Краткое описание изменений.

## Тип изменения

- [ ] 🐛 Bug fix (исправление ошибки)
- [ ] ✨ New feature (новая функциональность)
- [ ] 💥 Breaking change (критическое изменение)
- [ ] 📚 Documentation update (обновление документации)

## Тестирование

- [ ] Тесты проходят локально
- [ ] Добавлены новые тесты (если применимо)
- [ ] Тестирование вручную выполнено

## Checklist

- [ ] Код соответствует стандартам проекта
- [ ] Self-review выполнен
- [ ] Комментарии добавлены в сложные места
- [ ] Документация обновлена
- [ ] Нет предупреждений линтера
```

### Процесс ревью

1. **Автоматические проверки** должны пройти
2. **Минимум один reviewer** должен одобрить
3. **Maintainer** делает merge

### После merge

```bash
# Обновите ваш локальный main
git checkout main
git pull upstream main

# Удалите feature branch
git branch -d feature/your-feature-name
git push origin --delete feature/your-feature-name
```

## Тестирование

### Типы тестов

1. **Unit тесты** - тестируют отдельные компоненты
2. **Integration тесты** - тестируют взаимодействие компонентов
3. **Performance тесты** - проверяют производительность

### Написание тестов

```csharp
[TestClass]
public class MessageProcessorTests
{
    private Mock<ILogger<MessageProcessor>> _mockLogger;
    private Mock<IConfiguration> _mockConfig;
    private MessageProcessor _processor;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        _mockConfig = new Mock<IConfiguration>();
        _processor = new MessageProcessor(_mockLogger.Object, _mockConfig.Object);
    }

    [TestMethod]
    public async Task ProcessMessageAsync_ValidMessage_ReturnsSuccess()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = 1,
            Text = "Test message",
            Date = DateTime.UtcNow
        };

        // Act
        var result = await _processor.ProcessMessageAsync(message);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test message", result.Data.Text);
    }

    [TestMethod]
    public async Task ProcessMessageAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => _processor.ProcessMessageAsync(null)
        );
    }
}
```

### Запуск тестов

```bash
# Все тесты
dotnet test

# Конкретный проект
dotnet test Telegram.HostedApp.Tests/

# С покрытием кода
dotnet test --collect:"XPlat Code Coverage"

# Performance тесты
dotnet test --filter Category=Performance
```

## Документация

### Требования к документации

- **README.md** должен быть актуальным
- **API изменения** требуют обновления документации
- **Новые функции** должны быть документированы
- **Примеры использования** для сложных функций

### Формат документации

- **Markdown** для большинства документов
- **XML комментарии** для кода
- **Скриншоты** для UI изменений

### Обновление документации

```bash
# Локальная проверка документации
# Убедитесь, что ссылки работают и форматирование корректно

# Примеры кода должны работать
dotnet run --example-from-readme
```

## Вопросы и помощь

### Где получить помощь

1. **[GitHub Issues](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues)** - для вопросов и обсуждений
2. **[GitHub Discussions](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/discussions)** - для общих вопросов
3. **Email**: [your-email@example.com] - для конфиденциальных вопросов

### FAQ

**Q: Как настроить development окружение?**
A: Следуйте инструкциям в разделе [Настройка среды разработки](#настройка-среды-разработки).

**Q: Мой PR был отклонен, что делать?**
A: Прочитайте комментарии reviewer'а, внесите исправления и обновите PR.

**Q: Как запустить только один тест?**
A: `dotnet test --filter "TestMethodName"`

**Q: Нужно ли создавать issue перед PR?**
A: Для больших изменений - да. Для мелких fixes - можно сразу PR.

## Спасибо!

Спасибо за ваш вклад в Telegram Chat Archiver! Каждый вклад ценен, будь то исправление опечатки или добавление новой функции. 

Вместе мы делаем отличный инструмент для архивирования Telegram чатов! 🚀

---

*Этот документ может обновляться. Пожалуйста, проверяйте актуальную версию перед contributing.*