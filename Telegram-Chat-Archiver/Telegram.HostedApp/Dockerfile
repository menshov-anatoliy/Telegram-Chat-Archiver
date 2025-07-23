# Production-ready multi-stage Dockerfile для Telegram Chat Archiver
# Этап 1: Базовый образ для runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base

# Установка необходимых пакетов для безопасности и производительности
RUN apk add --no-cache \
    ca-certificates \
    tzdata \
    && update-ca-certificates

# Создание пользователя приложения для безопасности
RUN addgroup -g 1001 -S appgroup && adduser -u 1001 -S appuser -G appgroup

WORKDIR /app

# Создание директорий с правильными правами доступа
RUN mkdir -p /app/logs /app/archives /app/media /app/data \
    && chown -R appuser:appgroup /app

# Этап 2: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=1.0.0

WORKDIR /src

# Копирование файлов проекта для восстановления зависимостей
COPY ["Telegram.HostedApp/Telegram.HostedApp.csproj", "Telegram.HostedApp/"]
COPY ["Telegram.HostedApp.Tests/Telegram.HostedApp.Tests.csproj", "Telegram.HostedApp.Tests/"]

# Восстановление зависимостей
RUN dotnet restore "Telegram.HostedApp/Telegram.HostedApp.csproj"

# Копирование всего исходного кода
COPY . .

# Сборка приложения
WORKDIR "/src/Telegram.HostedApp"
RUN dotnet build "Telegram.HostedApp.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build \
    --no-restore \
    /p:Version=$BUILD_VERSION

# Этап 3: Тестирование
FROM build AS test
WORKDIR /src
RUN dotnet test "Telegram.HostedApp.Tests/Telegram.HostedApp.Tests.csproj" \
    --no-build \
    --configuration $BUILD_CONFIGURATION \
    --logger "trx;LogFileName=test_results.trx" \
    --collect:"XPlat Code Coverage"

# Этап 4: Публикация приложения
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=1.0.0

RUN dotnet publish "Telegram.HostedApp.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --self-contained false \
    /p:Version=$BUILD_VERSION \
    /p:PublishTrimmed=true \
    /p:PublishSingleFile=false

# Этап 5: Финальный production образ
FROM base AS final

# Переключение на пользователя приложения
USER appuser

WORKDIR /app

# Копирование опубликованного приложения
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Переменные окружения для production
ENV DOTNET_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    ASPNETCORE_URLS=http://+:8080 \
    TZ=UTC

# Настройка приложения
ENV TelegramConfig__SessionFile="/app/data/session.dat" \
    ArchiveConfig__OutputPath="/app/archives" \
    ArchiveConfig__MediaPath="/app/media" \
    ArchiveConfig__DatabasePath="/app/data/metadata.db" \
    ArchiveConfig__SyncStatePath="/app/data/sync_state.json" \
    ArchiveConfig__ArchiveIntervalMinutes=60 \
    ArchiveConfig__MaxRetryAttempts=3

# Добавление health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD ["dotnet", "/app/Telegram.HostedApp.dll", "--health-check"] || exit 1

# Volumes для persistent данных
VOLUME ["/app/archives", "/app/media", "/app/logs", "/app/data"]

# Порты
EXPOSE 8080

# Метки для образа
LABEL maintainer="Telegram Chat Archiver Team" \
      version="${BUILD_VERSION}" \
      description="Production-ready Telegram Chat Archiver" \
      org.opencontainers.image.title="Telegram Chat Archiver" \
      org.opencontainers.image.description="Фоновый сервис для архивирования Telegram сообщений" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.vendor="Telegram Chat Archiver Team"

# Точка входа с graceful shutdown
ENTRYPOINT ["dotnet", "Telegram.HostedApp.dll"]