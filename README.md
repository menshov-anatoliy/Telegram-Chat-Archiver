# Telegram Chat Archiver

Фоновый сервис для автоматического архивирования сообщений из Telegram чатов.

## Описание

Telegram Chat Archiver - это консольное приложение .NET 8, работающее как фоновый сервис, который автоматически архивирует сообщения из указанных Telegram чатов в файлы JSON с настраиваемыми интервалами.

## Возможности

- Фоновое архивирование Telegram сообщений в формате Markdown
- Поддержка различных типов сообщений (текст, изображения, документы, голос, видео, стикеры)
- Автоматическая загрузка и сохранение медиафайлов
- Структурированное логирование с помощью Serilog
- Отправка уведомлений об ошибках в Telegram чат
- Настраиваемые интервалы архивирования
- Поддержка Docker контейнеров
- Конфигурация через appsettings.json и переменные окружения
- Graceful shutdown и улучшенная обработка ошибок

## Конфигурация

### appsettings.json

```json
{
  "TelegramConfig": {
    "ApiId": 0,                    // API ID из my.telegram.org
    "ApiHash": "",                 // API Hash из my.telegram.org
    "PhoneNumber": "",             // Номер телефона для авторизации
    "SessionFile": "session.dat"   // Файл для хранения сессии
  },
  "ArchiveConfig": {
    "OutputPath": "archives",                                        // Папка для архивов
    "MediaPath": "media",                                           // Папка для медиафайлов
    "FileNameFormat": "{Date:yyyy-MM-dd}.md",                      // Формат имени файла (Markdown)
    "ArchiveIntervalMinutes": 60,  // Интервал архивирования в минутах
    "MaxMessagesPerFile": 1000,    // Максимальное количество сообщений в файле
    "TargetChat": "",              // Имя или ID чата для архивирования
    "ErrorNotificationChat": ""    // ID канала для уведомлений об ошибках
  }
}
```

### Переменные окружения

- `TelegramConfig__ApiId` - API ID
- `TelegramConfig__ApiHash` - API Hash
- `TelegramConfig__PhoneNumber` - Номер телефона
- `ArchiveConfig__OutputPath` - Путь к папке архивов
- `ArchiveConfig__TargetChat` - Имя или ID чата для архивирования
- `ArchiveConfig__ErrorNotificationChat` - ID канала для уведомлений об ошибках

## Запуск

### Локальный запуск

1. Склонируйте репозиторий
2. Настройте конфигурацию в `appsettings.json`
3. Запустите проект:

```bash
cd Telegram-Chat-Archiver/Telegram.HostedApp
dotnet run
```

### Docker

```bash
# Сборка образа
docker build -t telegram-archiver .

# Запуск контейнера
docker run -d \
  -e TelegramConfig__ApiId=YOUR_API_ID \
  -e TelegramConfig__ApiHash=YOUR_API_HASH \
  -e TelegramConfig__PhoneNumber=YOUR_PHONE_NUMBER \
  -v ./archives:/app/archives \
  -v ./logs:/app/logs \
  --name telegram-archiver \
  telegram-archiver
```

## Структура проекта

```
Telegram-Chat-Archiver/
├── Telegram.HostedApp/
│   ├── Configuration/          # Классы конфигурации
│   │   ├── TelegramConfig.cs
│   │   └── ArchiveConfig.cs
│   ├── Models/                 # Модели данных
│   │   └── ChatMessage.cs
│   ├── Services/               # Сервисы
│   │   ├── ITelegramArchiverService.cs
│   │   ├── TelegramArchiverServiceImpl.cs
│   │   ├── TelegramArchiverService.cs
│   │   ├── IMarkdownService.cs
│   │   ├── MarkdownService.cs
│   │   ├── IMediaDownloadService.cs
│   │   ├── MediaDownloadService.cs
│   │   ├── ITelegramNotificationService.cs
│   │   └── TelegramNotificationService.cs
│   ├── appsettings.json        # Конфигурация приложения
│   ├── Program.cs              # Точка входа
│   └── Dockerfile              # Docker файл
├── Telegram.HostedApp.Tests/   # Юнит тесты
└── README.md                   # Данный файл
```

## Логирование

Сервис использует Serilog для структурированного логирования:
- Логи выводятся в консоль и файлы
- Файлы логов ротируются ежедневно
- Настраиваемые уровни логирования

## Разработка

### Требования

- .NET 8 SDK
- Visual Studio 2022 или VS Code

### Тестирование

```bash
dotnet test
```

### Сборка

```bash
dotnet build
```

## Безопасность

- Не размещайте реальные API ключи в конфигурационных файлах
- Используйте переменные окружения для чувствительных данных
- Регулярно обновляйте зависимости

## Лицензия

Данный проект распространяется под лицензией MIT.

## Вклад в проект

1. Создайте форк проекта
2. Создайте ветку для новой функции (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте в ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request
Фоновый сервис для автоматического архивирования сообщений из указанного чата Telegram в локальные файлы формата Markdown с поддержкой медиафайлов
