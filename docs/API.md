# API Documentation

Документация для REST API Telegram Chat Archiver.

## Базовая информация

- **Base URL**: `http://localhost:8080`
- **Content-Type**: `application/json`
- **Authentication**: Не требуется (для внутреннего использования)

## Endpoints

### Health Check API

#### GET /health

Проверка общего состояния системы.

**Response:**
```json
{
  "status": "Healthy",
  "totalDuration": 45.23,
  "results": {
    "telegram": {
      "status": "Healthy",
      "description": "Telegram соединение активно",
      "duration": 12.45,
      "data": {}
    },
    "filesystem": {
      "status": "Healthy", 
      "description": "Все директории доступны для записи",
      "duration": 8.91,
      "data": {
        "checkedDirectories": ["ArchivesDirectory", "MediaDirectory"]
      }
    },
    "database": {
      "status": "Healthy",
      "description": "База данных доступна", 
      "duration": 5.67,
      "data": {
        "databaseExists": true,
        "syncStateExists": true,
        "databaseSize": 1024000
      }
    },
    "resources": {
      "status": "Healthy",
      "description": "Системные ресурсы в норме",
      "duration": 18.20,
      "data": {
        "memoryUsageMB": 256,
        "threadCount": 15,
        "handleCount": 342,
        "driveInfo": [
          {
            "name": "/",
            "totalSizeGB": 100,
            "availableFreeSpaceGB": 50,
            "usagePercentage": 50.0
          }
        ]
      }
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Status Codes:**
- `200 OK` - Система здорова
- `503 Service Unavailable` - Система нездорова

#### GET /health/live

Проверка жизнеспособности (liveness probe).

**Response:**
```json
{
  "status": "Healthy",
  "results": {
    "telegram": { /* ... */ },
    "resources": { /* ... */ }
  }
}
```

#### GET /health/ready

Проверка готовности к обслуживанию (readiness probe).

**Response:**
```json
{
  "status": "Healthy", 
  "results": {
    "telegram": { /* ... */ },
    "filesystem": { /* ... */ },
    "database": { /* ... */ }
  }
}
```

#### GET /health/startup

Проверка успешного запуска (startup probe).

**Response:**
```json
{
  "status": "Healthy",
  "results": {
    "filesystem": { /* ... */ }
  }
}
```

### Monitoring API

#### GET /api/monitoring/status

Получить детальный статус системы.

**Response:**
```json
{
  "status": "Healthy",
  "totalDuration": 45.23,
  "results": {
    // Аналогично /health
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/monitoring/statistics

Получить статистику обработки сообщений.

**Response:**
```json
{
  "messageStatistics": {
    "Text": 1500,
    "Photo": 250,
    "Video": 50,
    "Document": 75,
    "Voice": 30,
    "Other": 25
  },
  "authorStatistics": {
    "User1": 800,
    "User2": 650,
    "User3": 480
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/monitoring/info

Получить информацию о системе и приложении.

**Response:**
```json
{
  "version": "1.0.0",
  "framework": ".NET 8.0",
  "environment": "Production",
  "machineName": "telegram-archiver-7d8f9b-xyz",
  "processId": 1,
  "startTime": "2024-01-15T09:00:00Z",
  "uptime": "1.12:34:56.789",
  "workingSet": 268435456,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### GET /api/monitoring/metrics

Получить метрики в формате Prometheus.

**Content-Type**: `text/plain`

**Response:**
```prometheus
# HELP telegram_archiver_memory_usage_bytes Current memory usage in bytes
# TYPE telegram_archiver_memory_usage_bytes gauge
telegram_archiver_memory_usage_bytes 268435456

# HELP telegram_archiver_cpu_usage_seconds_total Total CPU usage in seconds  
# TYPE telegram_archiver_cpu_usage_seconds_total counter
telegram_archiver_cpu_usage_seconds_total 123.45

# HELP telegram_archiver_threads_count Current number of threads
# TYPE telegram_archiver_threads_count gauge
telegram_archiver_threads_count 15

# HELP telegram_archiver_handles_count Current number of handles
# TYPE telegram_archiver_handles_count gauge
telegram_archiver_handles_count 342

# HELP telegram_archiver_uptime_seconds Application uptime in seconds
# TYPE telegram_archiver_uptime_seconds gauge
telegram_archiver_uptime_seconds 5496

# HELP telegram_archiver_messages_total Total number of processed messages by type
# TYPE telegram_archiver_messages_total counter
telegram_archiver_messages_total{type="text"} 1500
telegram_archiver_messages_total{type="photo"} 250
telegram_archiver_messages_total{type="video"} 50
telegram_archiver_messages_total{type="document"} 75
telegram_archiver_messages_total{type="voice"} 30
telegram_archiver_messages_total{type="other"} 25

# HELP telegram_archiver_health_check Health check status (1=healthy, 0.5=degraded, 0=unhealthy)
# TYPE telegram_archiver_health_check gauge
telegram_archiver_health_check{check="telegram"} 1
telegram_archiver_health_check{check="filesystem"} 1  
telegram_archiver_health_check{check="database"} 1
telegram_archiver_health_check{check="resources"} 1

# HELP telegram_archiver_last_update_timestamp Timestamp of last metrics update
# TYPE telegram_archiver_last_update_timestamp gauge
telegram_archiver_last_update_timestamp 1705317000
```

## Статусы Health Checks

### Возможные статусы

- **Healthy** - Компонент работает нормально
- **Degraded** - Компонент работает с ограничениями 
- **Unhealthy** - Компонент не работает

### Детали проверок

#### telegram
- **Healthy**: Соединение с Telegram API активно
- **Unhealthy**: Нет соединения с Telegram API

#### filesystem  
- **Healthy**: Все необходимые директории доступны для записи
- **Degraded**: Некоторые директории недоступны
- **Unhealthy**: Критические директории недоступны

#### database
- **Healthy**: База данных и файлы состояния доступны
- **Unhealthy**: Ошибка доступа к базе данных

#### resources
- **Healthy**: Использование ресурсов в норме
- **Degraded**: Высокое использование памяти/диска (>90%)
- **Unhealthy**: Критическое использование ресурсов

## Коды ошибок

### HTTP Status Codes

- **200 OK** - Запрос выполнен успешно
- **500 Internal Server Error** - Внутренняя ошибка сервера
- **503 Service Unavailable** - Сервис недоступен

### Формат ошибок

```json
{
  "error": "Описание ошибки",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Использование в мониторинге

### Prometheus Configuration

```yaml
scrape_configs:
  - job_name: 'telegram-archiver'
    static_configs:
      - targets: ['telegram-archiver:8080']
    metrics_path: '/api/monitoring/metrics'
    scrape_interval: 30s
```

### Grafana Dashboard Queries

#### Memory Usage
```promql
telegram_archiver_memory_usage_bytes / 1024 / 1024
```

#### Message Processing Rate
```promql
rate(telegram_archiver_messages_total[5m])
```

#### Health Status
```promql
telegram_archiver_health_check
```

### Kubernetes Health Checks

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 60
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

startupProbe:
  httpGet:
    path: /health/startup
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10 
  failureThreshold: 12
```

## Примеры использования

### Bash/curl

```bash
# Проверка статуса
curl -f http://localhost:8080/health

# Получение статистики
curl -s http://localhost:8080/api/monitoring/statistics | jq .

# Мониторинг в цикле
while true; do
  curl -s http://localhost:8080/api/monitoring/status | jq '.status'
  sleep 30
done
```

### PowerShell

```powershell
# Проверка статуса
$response = Invoke-RestMethod -Uri "http://localhost:8080/health"
Write-Host "Status: $($response.status)"

# Получение метрик
$metrics = Invoke-RestMethod -Uri "http://localhost:8080/api/monitoring/metrics"
Write-Host $metrics
```

### Python

```python
import requests
import json

# Проверка статуса
response = requests.get('http://localhost:8080/api/monitoring/status')
status = response.json()
print(f"System status: {status['status']}")

# Мониторинг статистики
statistics = requests.get('http://localhost:8080/api/monitoring/statistics').json()
total_messages = sum(statistics['messageStatistics'].values())
print(f"Total messages processed: {total_messages}")
```

### C#

```csharp
using System.Text.Json;

var httpClient = new HttpClient();

// Проверка статуса
var response = await httpClient.GetAsync("http://localhost:8080/health");
var content = await response.Content.ReadAsStringAsync();
var status = JsonSerializer.Deserialize<HealthStatus>(content);

Console.WriteLine($"System status: {status.Status}");
```

## Rate Limiting

В настоящее время API не имеет ограничений по скорости запросов, но рекомендуется:

- **Health checks**: не чаще 1 раза в 10 секунд
- **Statistics**: не чаще 1 раза в 30 секунд  
- **Metrics**: не чаще 1 раза в 15 секунд

## Версионирование API

API следует семантическому версионированию:

- **Мажорные изменения** (breaking changes) увеличивают мажорную версию
- **Минорные изменения** (новые поля) увеличивают минорную версию
- **Исправления** увеличивают patch версию

Текущая версия API: **v1.0.0**

Изменения API документируются в [CHANGELOG.md](../CHANGELOG.md).