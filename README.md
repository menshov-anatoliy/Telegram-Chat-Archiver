# Telegram Chat Archiver

**Production-ready фоновый сервис для автоматического архивирования сообщений из Telegram чатов с enterprise-grade возможностями.**

[![CI/CD Pipeline](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/workflows/CI/CD%20Pipeline/badge.svg)](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/actions)
[![Docker](https://img.shields.io/badge/docker-ready-blue.svg)](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/pkgs/container/telegram-chat-archiver)
[![Kubernetes](https://img.shields.io/badge/kubernetes-ready-green.svg)](./k8s/)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

## 🚀 Основные возможности

### 📥 Архивирование
- **Автоматическое архивирование** сообщений из Telegram чатов в формате Markdown
- **Поддержка всех типов сообщений**: текст, изображения, документы, голос, видео, стикеры
- **Инкрементальная синхронизация** для эффективной обработки больших объемов данных
- **Автоматическая загрузка и сохранение медиафайлов**
- **Настраиваемые интервалы архивирования**

### 🖥️ Веб-интерфейс и мониторинг
- **Real-time dashboard** с метриками и графиками
- **Health checks** для мониторинга состояния системы
- **REST API** для интеграции с внешними системами
- **Responsive дизайн** с поддержкой мобильных устройств
- **Система уведомлений** в реальном времени

### 🏗️ Enterprise-grade архитектура
- **Масштабируемая архитектура** с поддержкой горизонтального масштабирования
- **Микросервисная структура** с четким разделением ответственности
- **Comprehensive логирование** с помощью Serilog
- **Graceful shutdown** и продвинутая обработка ошибок
- **Feature flags** и система миграций

### 🔒 Безопасность
- **Шифрование конфиденциальных данных** в состоянии покоя
- **Secure secrets management** с поддержкой внешних vault'ов
- **Input validation и sanitization** для всех входящих данных
- **Rate limiting** для защиты от злоупотреблений API
- **Audit логирование** всех критических операций

### 🚀 DevOps и развертывание
- **CI/CD Pipeline** с автоматическими тестами и деплоем
- **Docker контейнеризация** с multi-stage build
- **Kubernetes манифесты** для кластерного развертывания
- **Automated releases** с semantic versioning
- **Мониторинг и алертинг** с Prometheus и Grafana

## 📋 Содержание

- [Быстрый старт](#-быстрый-старт)
- [Установка и настройка](#-установка-и-настройка)
- [Конфигурация](#-конфигурация)
- [Развертывание](#-развертывание)
- [API документация](#-api-документация)
- [Мониторинг](#-мониторинг)
- [Безопасность](#-безопасность)
- [Разработка](#-разработка)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)

## ⚡ Быстрый старт

### Использование Docker (рекомендуется)

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/menshov-anatoliy/Telegram-Chat-Archiver.git
   cd Telegram-Chat-Archiver
   ```

2. **Создайте файл конфигурации:**
   ```bash
   cp .env.example .env
   # Отредактируйте .env файл с вашими настройками
   ```

3. **Запустите с помощью Docker Compose:**
   ```bash
   docker-compose up -d
   ```

4. **Откройте веб-интерфейс:**
   ```
   http://localhost:8080
   ```

### Локальный запуск

```bash
cd Telegram-Chat-Archiver/Telegram.HostedApp
dotnet run
```

## 🛠️ Установка и настройка

### Требования

- **.NET 8.0 SDK** или выше
- **Docker** (для контейнерного развертывания)
- **Kubernetes** (для кластерного развертывания)
- **Telegram API credentials** (получите на [my.telegram.org](https://my.telegram.org))

### Получение Telegram API credentials

1. Перейдите на [my.telegram.org](https://my.telegram.org)
2. Войдите с вашим номером телефона
3. Перейдите в раздел "API development tools"
4. Создайте новое приложение
5. Скопируйте `api_id` и `api_hash`

### Создание Telegram бота (опционально)

Для получения уведомлений об ошибках:

1. Найдите [@BotFather](https://t.me/botfather) в Telegram
2. Отправьте `/newbot` и следуйте инструкциям
3. Скопируйте токен бота

## ⚙️ Конфигурация

### Переменные окружения

Основные настройки через переменные окружения:

```bash
# Telegram Configuration (обязательные)
TELEGRAM_API_ID=12345678
TELEGRAM_API_HASH=0123456789abcdef0123456789abcdef
TELEGRAM_PHONE_NUMBER=+1234567890

# Archive Configuration
TELEGRAM_TARGET_CHAT=@mychannel
ARCHIVE_INTERVAL_MINUTES=60
MAX_MESSAGES_PER_FILE=1000

# Bot Configuration (опционально)
TELEGRAM_BOT_TOKEN=1234567890:AAHleM4zu6YfFADIiIt_lwDBgNP6IC6Z-KU
TELEGRAM_ADMIN_USER_ID=123456789
```

### appsettings.json

Полная конфигурация в файле `appsettings.json`:

```json
{
  "TelegramConfig": {
    "ApiId": 0,
    "ApiHash": "",
    "PhoneNumber": "",
    "SessionFile": "session.dat"
  },
  "BotConfig": {
    "BotToken": "",
    "AdminUserId": 0,
    "EnableBotNotifications": true,
    "EnableManagementCommands": true
  },
  "ArchiveConfig": {
    "OutputPath": "archives",
    "MediaPath": "media",
    "DatabasePath": "metadata.db",
    "FileNameFormat": "{Date:yyyy-MM-dd}.md",
    "ArchiveIntervalMinutes": 60,
    "MaxMessagesPerFile": 1000,
    "TargetChat": "",
    "EnableIncrementalSync": true,
    "EnableAutoTagging": true
  }
}
```

## 🚀 Развертывание

### Docker

#### Простое развертывание

```bash
docker run -d \
  -e TELEGRAM_API_ID=your_api_id \
  -e TELEGRAM_API_HASH=your_api_hash \
  -e TELEGRAM_PHONE_NUMBER=your_phone \
  -e TELEGRAM_TARGET_CHAT=@yourchannel \
  -v ./archives:/app/archives \
  -v ./media:/app/media \
  -v ./logs:/app/logs \
  -p 8080:8080 \
  --name telegram-archiver \
  ghcr.io/menshov-anatoliy/telegram-chat-archiver:latest
```

#### Docker Compose

```bash
# Для разработки
docker-compose up -d

# Для продакшена
docker-compose -f docker-compose.prod.yml up -d
```

### Kubernetes

```bash
# Применить все манифесты
kubectl apply -f k8s/

# Проверить статус
kubectl get pods -n telegram-archiver
```

### Проверка состояния развертывания

```bash
# Health check
curl http://localhost:8080/health

# Статус системы
curl http://localhost:8080/api/monitoring/status

# Метрики
curl http://localhost:8080/api/monitoring/metrics
```

## 📊 API документация

### Health Check Endpoints

| Endpoint | Описание |
|----------|----------|
| `GET /health` | Общий статус системы |
| `GET /health/live` | Проверка жизнеспособности |
| `GET /health/ready` | Готовность к обслуживанию |
| `GET /health/startup` | Проверка запуска |

### Monitoring API

| Endpoint | Описание |
|----------|----------|
| `GET /api/monitoring/status` | Детальный статус системы |
| `GET /api/monitoring/statistics` | Статистика обработки |
| `GET /api/monitoring/info` | Информация о системе |
| `GET /api/monitoring/metrics` | Метрики Prometheus |

### Примеры ответов

#### GET /api/monitoring/status

```json
{
  "status": "Healthy",
  "totalDuration": 45.23,
  "results": {
    "telegram": {
      "status": "Healthy",
      "description": "Telegram соединение активно",
      "duration": 12.45
    },
    "filesystem": {
      "status": "Healthy",
      "description": "Все директории доступны для записи",
      "duration": 8.91
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 📈 Мониторинг

### Встроенный Dashboard

Откройте `http://localhost:8080` для доступа к веб-интерфейсу с:

- **Real-time метрики** системы
- **Health checks** статусы
- **Графики обработки** сообщений
- **Системная информация**
- **Лог активности**

### Prometheus интеграция

Метрики доступны по адресу `/api/monitoring/metrics`:

```prometheus
# Использование памяти
telegram_archiver_memory_usage_bytes

# Количество обработанных сообщений
telegram_archiver_messages_total{type="text"}

# Статус health checks
telegram_archiver_health_check{check="telegram"}
```

### Grafana Dashboard

При использовании `docker-compose.prod.yml` Grafana доступна по адресу:
- URL: `http://localhost:3000`
- Логин: `admin`
- Пароль: `admin` (или из переменной `GRAFANA_ADMIN_PASSWORD`)

## 🔐 Безопасность

### Рекомендации по безопасности

1. **Не размещайте API ключи в коде**
   ```bash
   # Используйте переменные окружения
   export TELEGRAM_API_HASH=your_secret_hash
   ```

2. **Ограничьте сетевой доступ**
   ```yaml
   # В docker-compose.prod.yml
   ports:
     - "127.0.0.1:8080:8080"  # Только localhost
   ```

3. **Регулярно обновляйте зависимости**
   ```bash
   dotnet list package --outdated
   ```

4. **Используйте HTTPS в продакшене**
   - Настройте reverse proxy (nginx/traefik)
   - Получите SSL сертификат (Let's Encrypt)

### Аудит безопасности

```bash
# Сканирование уязвимостей
dotnet list package --vulnerable

# Проверка Docker образа
docker scout cves telegram-archiver:latest
```

## 🔧 Разработка

### Настройка среды разработки

```bash
# Клонирование
git clone https://github.com/menshov-anatoliy/Telegram-Chat-Archiver.git
cd Telegram-Chat-Archiver

# Восстановление зависимостей
dotnet restore

# Сборка
dotnet build

# Запуск тестов
dotnet test
```

### Структура проекта

```
Telegram-Chat-Archiver/
├── .github/workflows/       # CI/CD конфигурация
├── k8s/                     # Kubernetes манифесты
├── scripts/                 # Утилиты и скрипты
├── Telegram.HostedApp/      # Основное приложение
│   ├── Configuration/       # Классы конфигурации
│   ├── Controllers/         # API контроллеры
│   ├── HealthChecks/        # Health check компоненты
│   ├── Models/              # Модели данных
│   ├── Services/            # Бизнес-логика
│   └── wwwroot/            # Статические файлы веб-интерфейса
├── Telegram.HostedApp.Tests/ # Тесты
│   └── Performance/         # Нагрузочные тесты
├── docker-compose.yml       # Локальное развертывание
├── docker-compose.prod.yml  # Production развертывание
└── .env.example            # Пример конфигурации
```

### Добавление новых функций

1. **Создайте ветку:**
   ```bash
   git checkout -b feature/amazing-feature
   ```

2. **Реализуйте изменения:**
   - Добавьте тесты
   - Обновите документацию
   - Следуйте code style

3. **Запустите тесты:**
   ```bash
   dotnet test
   ```

4. **Создайте Pull Request**

### Code Style

Проект использует стандартные .NET конвенции:
- **Использование** `PascalCase` для публичных методов
- **Использование** `camelCase` для приватных полей
- **Комментарии на русском языке**
- **XML документация** для публичных API

## 🚨 Troubleshooting

### Частые проблемы

#### 1. Ошибка авторизации Telegram

```
Error: Could not authenticate with Telegram
```

**Решение:**
- Проверьте правильность `API_ID` и `API_HASH`
- Убедитесь, что номер телефона указан с кодом страны
- Удалите файл сессии `session.dat` для повторной авторизации

#### 2. Недостаточно прав для записи

```
Error: Access denied to directory /app/archives
```

**Решение:**
```bash
# Docker
docker run --user $(id -u):$(id -g) ...

# Локально
chmod 755 archives/ media/ logs/
```

#### 3. Высокое использование памяти

```
Warning: Memory usage exceeded 800MB
```

**Решение:**
- Уменьшите `BatchSize` в конфигурации
- Включите `EnableLazyMediaDownload`
- Ограничьте `UserCacheSize`

#### 4. Проблемы с Docker контейнером

```bash
# Проверить логи
docker logs telegram-archiver

# Войти в контейнер
docker exec -it telegram-archiver /bin/bash

# Проверить health check
docker inspect telegram-archiver | grep Health
```

### Диагностика

#### Проверка состояния системы

```bash
# Health check
curl -f http://localhost:8080/health || echo "Service is down"

# Детальный статус
curl -s http://localhost:8080/api/monitoring/status | jq .

# Системная информация
curl -s http://localhost:8080/api/monitoring/info | jq .
```

#### Логи

```bash
# Docker логи
docker logs -f telegram-archiver

# Локальные логи
tail -f logs/telegram-archiver-*.log

# Kubernetes логи
kubectl logs -f deployment/telegram-archiver -n telegram-archiver
```

### Получение помощи

1. **Проверьте [Issues](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues)**
2. **Создайте новый issue** с подробным описанием проблемы
3. **Приложите логи** и конфигурацию (без секретных данных)

## 🤝 Contributing

Мы приветствуем вклад в развитие проекта!

### Как внести вклад

1. **Fork репозиторий**
2. **Создайте feature branch** (`git checkout -b feature/AmazingFeature`)
3. **Commit изменения** (`git commit -m 'Add some AmazingFeature'`)
4. **Push в branch** (`git push origin feature/AmazingFeature`)
5. **Откройте Pull Request**

### Правила разработки

- **Все комментарии на русском языке**
- **Тесты обязательны** для новой функциональности
- **Обновляйте документацию** при изменении API
- **Следуйте существующему code style**
- **Один PR = одна функция/исправление**

### Типы изменений

- 🐛 **Bug fix** - исправление ошибок
- ✨ **New feature** - новая функциональность
- 📚 **Documentation** - обновление документации
- 🚀 **Performance** - улучшение производительности
- 🔧 **Maintenance** - техническое обслуживание

## 📄 Лицензия

Данный проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE) для подробностей.

## 🙏 Благодарности

- **Telegram** за предоставление API
- **WTelegramClient** за отличную .NET библиотеку
- **Сообщество .NET** за инструменты и поддержку
- **Все контрибьюторы** проекта

---

<div align="center">

**Сделано с ❤️ для сообщества**

[⭐ Star](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/stargazers) | [🐛 Report Bug](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues) | [💡 Request Feature](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues)

</div>
