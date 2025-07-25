# Конфигурация через переменные окружения

## Описание

Telegram Chat Archiver теперь поддерживает конфигурацию через переменные окружения, что особенно полезно для развертывания в Docker контейнерах и облачных средах.

## Переменные окружения

### Конфигурация Telegram API

- `TelegramConfig__ApiId` - API ID приложения из my.telegram.org
- `TelegramConfig__ApiHash` - API Hash приложения из my.telegram.org  
- `TelegramConfig__PhoneNumber` - Номер телефона для авторизации
- `TelegramConfig__SessionFile` - Путь к файлу сессии (по умолчанию: "session.dat")

### Конфигурация архивирования

- `ArchiveConfig__OutputPath` - Путь к папке для сохранения архивов (по умолчанию: "archives")
- `ArchiveConfig__MediaPath` - Путь к папке для сохранения медиафайлов (по умолчанию: "media")
- `ArchiveConfig__TargetChat` - Имя или ID чата для архивирования
- `ArchiveConfig__ArchiveIntervalMinutes` - Интервал проверки новых сообщений в минутах (по умолчанию: 60)
- `ArchiveConfig__ReportIntervalMinutes` - Интервал создания отчетов в минутах (по умолчанию: 1440)
- `ArchiveConfig__MaxMessagesPerFile` - Максимальное количество сообщений в одном файле (по умолчанию: 1000)
- `ArchiveConfig__EnableIncrementalSync` - Включить инкрементальную синхронизацию (по умолчанию: true)

### Конфигурация бота

- `BotConfig__BotToken` - Токен Telegram бота для уведомлений
- `BotConfig__EnableManagementCommands` - Включить команды управления через бота (по умолчанию: false)

## Примеры использования

### Docker

```bash
docker run -d \
  -e TelegramConfig__ApiId=12345 \
  -e TelegramConfig__ApiHash=your_api_hash \
  -e TelegramConfig__PhoneNumber=+1234567890 \
  -e ArchiveConfig__TargetChat="My Chat" \
  -e ArchiveConfig__OutputPath=/app/archives \
  -v ./archives:/app/archives \
  telegram-chat-archiver
```

### Docker Compose

```yaml
version: '3.8'
services:
  telegram-archiver:
    image: telegram-chat-archiver
    environment:
      - TelegramConfig__ApiId=12345
      - TelegramConfig__ApiHash=your_api_hash
      - TelegramConfig__PhoneNumber=+1234567890
      - ArchiveConfig__TargetChat=My Chat
      - ArchiveConfig__OutputPath=/app/archives
      - ArchiveConfig__MediaPath=/app/media
    volumes:
      - ./archives:/app/archives
      - ./media:/app/media
      - ./session:/app/session
```

### Системные переменные окружения (Linux)

```bash
export TelegramConfig__ApiId=12345
export TelegramConfig__ApiHash=your_api_hash
export TelegramConfig__PhoneNumber=+1234567890
export ArchiveConfig__TargetChat="My Chat"
dotnet run
```

### PowerShell (Windows)

```powershell
$env:TelegramConfig__ApiId="12345"
$env:TelegramConfig__ApiHash="your_api_hash"
$env:TelegramConfig__PhoneNumber="+1234567890"
$env:ArchiveConfig__TargetChat="My Chat"
dotnet run
```

## Приоритет конфигурации

Конфигурация загружается в следующем порядке приоритета (последнее перезаписывает предыдущее):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. **Переменные окружения** (наивысший приоритет)

## Важные замечания

- Переменные окружения имеют наивысший приоритет и перезаписывают настройки из файлов конфигурации
- Для вложенных свойств используется двойное подчеркивание `__` как разделитель
- Чувствительные данные (токены, хеши) логируются в замаскированном виде
- Все переменные являются опциональными - если не заданы, используются значения из файлов конфигурации

## Режим реального времени

Начиная с данной версии, архивер работает в режиме реального времени с инкрементальным добавлением сообщений:

- Интервал проверки новых сообщений: 30 секунд (можно изменить через `ArchiveConfig__ArchiveIntervalMinutes`)
- Файлы создаются с именами по дате: `ГГГГ-ММ-ДД.md`
- Сообщения дописываются в конец файла (`File.AppendAllText`)
- Потокобезопасная запись с использованием `lock`
- Механизм `ThrowPendingUpdates = true` обрабатывает пропущенные сообщения автоматически