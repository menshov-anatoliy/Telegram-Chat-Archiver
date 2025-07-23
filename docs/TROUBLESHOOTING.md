# Troubleshooting Guide

Руководство по решению распространенных проблем при использовании Telegram Chat Archiver.

## 📋 Содержание

- [Общие проблемы](#общие-проблемы)
- [Проблемы авторизации](#проблемы-авторизации)
- [Проблемы с файловой системой](#проблемы-с-файловой-системой)
- [Проблемы производительности](#проблемы-производительности)
- [Docker проблемы](#docker-проблемы)
- [Kubernetes проблемы](#kubernetes-проблемы)
- [Проблемы с API](#проблемы-с-api)
- [Диагностика](#диагностика)
- [Логи и мониторинг](#логи-и-мониторинг)
- [Получение помощи](#получение-помощи)

## Общие проблемы

### ❌ Приложение не запускается

**Симптомы:**
```
Application failed to start
System.ArgumentNullException: Value cannot be null
```

**Причины и решения:**

1. **Неправильная конфигурация:**
   ```bash
   # Проверьте обязательные переменные
   echo $TELEGRAM_API_ID
   echo $TELEGRAM_API_HASH
   echo $TELEGRAM_PHONE_NUMBER
   ```

2. **Отсутствующие зависимости:**
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Проблемы с правами доступа:**
   ```bash
   # Linux/macOS
   chmod 755 ./archives ./media ./logs
   
   # Windows (PowerShell as Admin)
   icacls .\archives /grant Everyone:F
   ```

### ❌ Высокое использование ресурсов

**Симптомы:**
- Приложение потребляет много памяти (>1GB)
- Высокая нагрузка на CPU
- Медленная обработка сообщений

**Решения:**

1. **Оптимизация конфигурации:**
   ```json
   {
     "ArchiveConfig": {
       "BatchSize": 50,           // Уменьшить размер пакета
       "UserCacheSize": 1000,     // Уменьшить размер кэша
       "EnableLazyMediaDownload": true,  // Включить ленивую загрузку
       "MaxMessagesPerFile": 500  // Уменьшить размер файлов
     }
   }
   ```

2. **Docker ограничения:**
   ```yaml
   deploy:
     resources:
       limits:
         memory: 512M
         cpus: '0.5'
       reservations:
         memory: 256M
         cpus: '0.25'
   ```

### ❌ Медленная обработка сообщений

**Симптомы:**
- Архивирование занимает слишком много времени
- Сообщения обрабатываются по одному

**Решения:**

1. **Увеличение размера пакета:**
   ```json
   {
     "ArchiveConfig": {
       "BatchSize": 200,
       "ArchiveIntervalMinutes": 30
     }
   }
   ```

2. **Параллельная обработка:**
   ```bash
   # Проверить количество потоков
   docker exec telegram-archiver ps -T
   ```

## Проблемы авторизации

### ❌ Ошибка авторизации Telegram

**Симптомы:**
```
TelegramClient: Could not authenticate
FloodWaitException: Must wait X seconds
```

**Решения:**

1. **Проверка учетных данных:**
   ```bash
   # Убедитесь, что API_ID и API_HASH корректны
   curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getMe"
   ```

2. **Сброс сессии:**
   ```bash
   # Удалить файл сессии для повторной авторизации
   rm session.dat
   
   # Docker
   docker exec telegram-archiver rm /app/data/session.dat
   docker restart telegram-archiver
   ```

3. **Flood protection:**
   ```bash
   # При получении FloodWaitException подождите указанное время
   # Затем перезапустите приложение
   sleep 300  # 5 минут
   ```

### ❌ Двухфакторная аутентификация

**Симптомы:**
```
PasswordNeededException: Account has 2FA enabled
```

**Решение:**
```bash
# Запустите приложение в интерактивном режиме
docker run -it telegram-archiver

# Введите пароль 2FA когда будет запрошен
```

### ❌ Неверный номер телефона

**Симптомы:**
```
PhoneNumberInvalidException: Invalid phone number
```

**Решение:**
```bash
# Формат номера: +{country_code}{phone_number}
# Примеры:
export TELEGRAM_PHONE_NUMBER="+1234567890"    # США
export TELEGRAM_PHONE_NUMBER="+79001234567"   # Россия
export TELEGRAM_PHONE_NUMBER="+380987654321"  # Украина
```

## Проблемы с файловой системой

### ❌ Недостаточно прав доступа

**Симптомы:**
```
UnauthorizedAccessException: Access to the path is denied
System.IO.DirectoryNotFoundException
```

**Решения:**

1. **Linux/macOS:**
   ```bash
   # Изменить владельца директорий
   sudo chown -R $USER:$USER ./archives ./media ./logs
   
   # Установить права доступа
   chmod -R 755 ./archives ./media ./logs
   
   # Docker с правильным пользователем
   docker run --user $(id -u):$(id -g) telegram-archiver
   ```

2. **Windows:**
   ```powershell
   # Установить полные права для текущего пользователя
   icacls .\archives /grant $env:USERNAME:F /T
   icacls .\media /grant $env:USERNAME:F /T
   icacls .\logs /grant $env:USERNAME:F /T
   ```

### ❌ Заполнено дисковое пространство

**Симптомы:**
```
IOException: There is not enough space on the disk
```

**Решения:**

1. **Очистка старых архивов:**
   ```bash
   # Найти большие файлы
   find ./archives -size +100M -ls
   
   # Удалить старые архивы (старше 30 дней)
   find ./archives -name "*.md" -mtime +30 -delete
   ```

2. **Настройка ротации:**
   ```json
   {
     "ArchiveConfig": {
       "LogRetentionDays": 7,
       "MaxLogFileSizeMB": 50
     }
   }
   ```

3. **Мониторинг места:**
   ```bash
   # Создать cron задачу для мониторинга
   echo "0 */6 * * * df -h | grep -E '9[0-9]%' && echo 'Disk space warning'" | crontab -
   ```

### ❌ Проблемы с кодировкой

**Симптомы:**
- Кракозябры в архивных файлах
- Неправильное отображение emoji

**Решение:**
```bash
# Установить правильную локаль
export LC_ALL=en_US.UTF-8
export LANG=en_US.UTF-8

# Docker
docker run -e LC_ALL=en_US.UTF-8 -e LANG=en_US.UTF-8 telegram-archiver
```

## Проблемы производительности

### ❌ Утечки памяти

**Симптомы:**
- Постоянный рост потребления памяти
- OutOfMemoryException

**Диагностика:**
```bash
# Мониторинг использования памяти
while true; do
  echo "$(date): $(docker stats telegram-archiver --no-stream --format 'table {{.MemUsage}}')"
  sleep 60
done
```

**Решения:**

1. **Настройка GC:**
   ```bash
   export DOTNET_gcServer=1
   export DOTNET_GCConserveMemory=5
   ```

2. **Ограничение ресурсов:**
   ```yaml
   # docker-compose.yml
   deploy:
     resources:
       limits:
         memory: 1G
   ```

### ❌ Медленная загрузка медиафайлов

**Симптомы:**
- Долгое время архивирования чатов с медиафайлами
- Timeouts при загрузке

**Решения:**

1. **Ленивая загрузка:**
   ```json
   {
     "ArchiveConfig": {
       "EnableLazyMediaDownload": true,
       "MediaDownloadTimeout": 30000
     }
   }
   ```

2. **Параллельная загрузка:**
   ```json
   {
     "ArchiveConfig": {
       "MaxConcurrentDownloads": 3
     }
   }
   ```

## Docker проблемы

### ❌ Контейнер не запускается

**Симптомы:**
```bash
docker: Error response from daemon: container failed to start
```

**Диагностика:**
```bash
# Проверить логи контейнера
docker logs telegram-archiver

# Проверить статус
docker ps -a

# Войти в контейнер для отладки
docker run -it --entrypoint /bin/bash telegram-archiver
```

**Решения:**

1. **Проблемы с правами:**
   ```bash
   # Запуск с правильным пользователем
   docker run --user 1001:1001 telegram-archiver
   ```

2. **Проблемы с volumes:**
   ```bash
   # Создать директории заранее
   mkdir -p ./archives ./media ./logs ./data
   chmod 755 ./archives ./media ./logs ./data
   ```

### ❌ Health check провалы

**Симптомы:**
```bash
docker inspect telegram-archiver | grep -A 5 Health
# "Status": "unhealthy"
```

**Решения:**

1. **Увеличить timeout:**
   ```dockerfile
   HEALTHCHECK --interval=30s --timeout=30s --start-period=120s --retries=3 \
       CMD ["dotnet", "/app/Telegram.HostedApp.dll", "--health-check"] || exit 1
   ```

2. **Отладка health check:**
   ```bash
   # Выполнить health check вручную
   docker exec telegram-archiver dotnet /app/Telegram.HostedApp.dll --health-check
   ```

### ❌ Проблемы с сетью

**Симптомы:**
- Контейнер не может подключиться к Telegram API
- DNS resolution failures

**Решения:**

1. **Проверка сети:**
   ```bash
   # Тест подключения
   docker exec telegram-archiver ping api.telegram.org
   
   # Проверка DNS
   docker exec telegram-archiver nslookup api.telegram.org
   ```

2. **Настройка DNS:**
   ```yaml
   # docker-compose.yml
   services:
     telegram-archiver:
       dns:
         - 8.8.8.8
         - 8.8.4.4
   ```

## Kubernetes проблемы

### ❌ Pod не запускается

**Симптомы:**
```bash
kubectl get pods -n telegram-archiver
# NAME                                 READY   STATUS    RESTARTS   AGE
# telegram-archiver-xxx                0/1     Pending   0          5m
```

**Диагностика:**
```bash
# Описание Pod
kubectl describe pod telegram-archiver-xxx -n telegram-archiver

# Логи Pod
kubectl logs telegram-archiver-xxx -n telegram-archiver

# События в namespace
kubectl get events -n telegram-archiver --sort-by='.lastTimestamp'
```

**Решения:**

1. **Проблемы с ресурсами:**
   ```yaml
   # Уменьшить запросы ресурсов
   resources:
     requests:
       memory: "128Mi"
       cpu: "100m"
   ```

2. **Проблемы с PVC:**
   ```bash
   # Проверить PVC
   kubectl get pvc -n telegram-archiver
   
   # Описание PVC
   kubectl describe pvc telegram-archiver-data -n telegram-archiver
   ```

### ❌ Проблемы с secrets

**Симптомы:**
```bash
kubectl logs telegram-archiver-xxx -n telegram-archiver
# Error: configuration value is empty
```

**Решения:**

1. **Проверка secrets:**
   ```bash
   # Список secrets
   kubectl get secrets -n telegram-archiver
   
   # Содержимое secret (base64 encoded)
   kubectl get secret telegram-archiver-secrets -o yaml -n telegram-archiver
   ```

2. **Обновление secrets:**
   ```bash
   # Удалить и пересоздать secret
   kubectl delete secret telegram-archiver-secrets -n telegram-archiver
   kubectl apply -f k8s/secret.yaml
   
   # Перезапустить deployment
   kubectl rollout restart deployment/telegram-archiver -n telegram-archiver
   ```

## Проблемы с API

### ❌ API недоступен

**Симптомы:**
```bash
curl http://localhost:8080/health
# curl: (7) Failed to connect to localhost port 8080: Connection refused
```

**Решения:**

1. **Проверка порта:**
   ```bash
   # Проверить, слушает ли приложение порт
   netstat -tulpn | grep 8080
   
   # Docker
   docker port telegram-archiver
   ```

2. **Проблемы с binding:**
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Http": {
           "Url": "http://0.0.0.0:8080"
         }
       }
     }
   }
   ```

### ❌ Медленные API ответы

**Симптомы:**
- API запросы занимают много времени
- Timeouts при обращении к endpoints

**Решения:**

1. **Мониторинг производительности:**
   ```bash
   # Измерить время ответа
   time curl -s http://localhost:8080/api/monitoring/status > /dev/null
   ```

2. **Оптимизация:**
   ```json
   {
     "ArchiveConfig": {
       "EnableCaching": true,
       "CacheExpirationMinutes": 5
     }
   }
   ```

## Диагностика

### Системная информация

```bash
# Общая информация о системе
curl -s http://localhost:8080/api/monitoring/info | jq .

# Статус всех компонентов
curl -s http://localhost:8080/api/monitoring/status | jq '.results'

# Метрики производительности
curl -s http://localhost:8080/api/monitoring/metrics | grep memory
```

### Проверка конфигурации

```bash
# Проверить переменные окружения
env | grep TELEGRAM

# Docker
docker exec telegram-archiver env | grep TELEGRAM

# Kubernetes
kubectl exec deployment/telegram-archiver -n telegram-archiver -- env | grep TELEGRAM
```

### Тестирование соединения

```bash
# Тест DNS
nslookup api.telegram.org

# Тест HTTPS соединения
openssl s_client -connect api.telegram.org:443 -servername api.telegram.org

# Тест через curl
curl -I https://api.telegram.org
```

## Логи и мониторинг

### Уровни логирования

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",  // Для отладки
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Полезные команды для логов

```bash
# Фильтрация логов по уровню
docker logs telegram-archiver 2>&1 | grep -E "(ERR|WARN)"

# Последние ошибки
docker logs telegram-archiver 2>&1 | grep "ERROR" | tail -10

# Kubernetes логи
kubectl logs -f deployment/telegram-archiver -n telegram-archiver --since=1h

# Grep определенных событий
kubectl logs deployment/telegram-archiver -n telegram-archiver | grep "archived messages"
```

### Мониторинг в реальном времени

```bash
# Dashboard в терминале
watch -n 5 'curl -s http://localhost:8080/api/monitoring/status | jq ".status, .totalDuration"'

# Метрики памяти
watch -n 10 'docker stats telegram-archiver --no-stream --format "table {{.MemUsage}}\t{{.CPUPerc}}"'
```

## Производительность под нагрузкой

### Нагрузочное тестирование

```bash
# Простой нагрузочный тест API
for i in {1..100}; do
  curl -s http://localhost:8080/health > /dev/null &
done
wait

# Мониторинг во время нагрузки
while true; do
  echo "$(date): Response time: $(curl -w "@curl-format.txt" -s http://localhost:8080/health -o /dev/null)"
  sleep 1
done
```

### Оптимизация производительности

1. **Настройка .NET:**
   ```bash
   export DOTNET_GCConserveMemory=5
   export DOTNET_gcServer=1
   export DOTNET_TieredPGO=1
   ```

2. **Настройка Kestrel:**
   ```json
   {
     "Kestrel": {
       "Limits": {
         "MaxConcurrentConnections": 100,
         "MaxRequestBodySize": 10485760
       }
     }
   }
   ```

## Получение помощи

### Сбор диагностической информации

Перед обращением за помощью соберите следующую информацию:

```bash
# Создать диагностический отчет
cat > diagnostic-report.txt << EOF
=== System Information ===
$(uname -a)
$(docker --version)
$(dotnet --version)

=== Application Status ===
$(curl -s http://localhost:8080/api/monitoring/status 2>/dev/null || echo "API unavailable")

=== Docker Information ===
$(docker inspect telegram-archiver 2>/dev/null || echo "Container not found")

=== Recent Logs ===
$(docker logs telegram-archiver --tail 50 2>/dev/null || echo "No logs available")

=== Configuration ===
$(docker exec telegram-archiver env | grep -E "(TELEGRAM|ARCHIVE)" 2>/dev/null || echo "Config unavailable")
EOF
```

### Где получить помощь

1. **GitHub Issues**: [Создать issue](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/issues/new)
2. **GitHub Discussions**: [Общие вопросы](https://github.com/menshov-anatoliy/Telegram-Chat-Archiver/discussions)
3. **Email**: support@telegram-archiver.example.com

### Шаблон для bug report

```markdown
## Описание проблемы
Краткое описание проблемы

## Шаги для воспроизведения
1. Шаг 1
2. Шаг 2
3. Шаг 3

## Ожидаемое поведение
Что должно происходить

## Фактическое поведение
Что происходит на самом деле

## Окружение
- OS: [Ubuntu 22.04 / Windows 11 / macOS 13]
- .NET Version: [8.0.1]
- Docker Version: [24.0.7]
- Application Version: [1.0.0]

## Логи
```
Вставьте релевантные логи здесь (удалите конфиденциальные данные)
```

## Дополнительная информация
Любая дополнительная информация, которая может помочь
```

---

*Если вы нашли решение проблемы, которой нет в этом руководстве, пожалуйста, создайте PR для обновления документации!*