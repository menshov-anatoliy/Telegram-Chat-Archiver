#!/bin/bash

# Telegram Chat Archiver - Backup Script
# Скрипт для автоматического резервного копирования данных

set -euo pipefail

# Конфигурация
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_DIR="${BACKUP_DIR:-/opt/telegram-archiver/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Функции логирования
log_info() {
    echo -e "${GREEN}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"
}

# Функция очистки при выходе
cleanup() {
    if [[ -n "${TEMP_DIR:-}" && -d "$TEMP_DIR" ]]; then
        rm -rf "$TEMP_DIR"
    fi
}
trap cleanup EXIT

# Проверка зависимостей
check_dependencies() {
    local deps=("tar" "gzip" "docker" "du")
    for dep in "${deps[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            log_error "Зависимость '$dep' не найдена"
            exit 1
        fi
    done
}

# Создание директории для backup
create_backup_dir() {
    if [[ ! -d "$BACKUP_DIR" ]]; then
        log_info "Создание директории backup: $BACKUP_DIR"
        mkdir -p "$BACKUP_DIR"
    fi
}

# Backup архивов
backup_archives() {
    log_info "Создание backup архивов..."
    
    local archive_source="/opt/telegram-archiver/archives"
    local archive_backup="$TEMP_DIR/archives.tar.gz"
    
    if [[ -d "$archive_source" ]]; then
        tar -czf "$archive_backup" -C "$(dirname "$archive_source")" "$(basename "$archive_source")"
        local size=$(du -h "$archive_backup" | cut -f1)
        log_info "Backup архивов создан: $size"
    else
        log_warn "Директория архивов не найдена: $archive_source"
    fi
}

# Backup медиафайлов
backup_media() {
    log_info "Создание backup медиафайлов..."
    
    local media_source="/opt/telegram-archiver/media"
    local media_backup="$TEMP_DIR/media.tar.gz"
    
    if [[ -d "$media_source" ]]; then
        tar -czf "$media_backup" -C "$(dirname "$media_source")" "$(basename "$media_source")"
        local size=$(du -h "$media_backup" | cut -f1)
        log_info "Backup медиафайлов создан: $size"
    else
        log_warn "Директория медиафайлов не найдена: $media_source"
    fi
}

# Backup данных приложения
backup_data() {
    log_info "Создание backup данных приложения..."
    
    local data_source="/opt/telegram-archiver/data"
    local data_backup="$TEMP_DIR/data.tar.gz"
    
    if [[ -d "$data_source" ]]; then
        tar -czf "$data_backup" -C "$(dirname "$data_source")" "$(basename "$data_source")"
        local size=$(du -h "$data_backup" | cut -f1)
        log_info "Backup данных создан: $size"
    else
        log_warn "Директория данных не найдена: $data_source"
    fi
}

# Backup конфигурации
backup_config() {
    log_info "Создание backup конфигурации..."
    
    local config_backup="$TEMP_DIR/config.tar.gz"
    local config_files=()
    
    # Добавляем конфигурационные файлы
    [[ -f "/opt/telegram-archiver/appsettings.production.json" ]] && config_files+=("/opt/telegram-archiver/appsettings.production.json")
    [[ -f "/opt/telegram-archiver/.env" ]] && config_files+=("/opt/telegram-archiver/.env")
    [[ -f "/opt/telegram-archiver/docker-compose.prod.yml" ]] && config_files+=("/opt/telegram-archiver/docker-compose.prod.yml")
    
    if [[ ${#config_files[@]} -gt 0 ]]; then
        tar -czf "$config_backup" "${config_files[@]}"
        local size=$(du -h "$config_backup" | cut -f1)
        log_info "Backup конфигурации создан: $size"
    else
        log_warn "Конфигурационные файлы не найдены"
    fi
}

# Backup Docker состояния
backup_docker_state() {
    log_info "Создание backup Docker состояния..."
    
    local docker_backup="$TEMP_DIR/docker_state.txt"
    
    {
        echo "=== Docker Images ==="
        docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.ID}}\t{{.CreatedAt}}\t{{.Size}}"
        echo ""
        echo "=== Docker Containers ==="
        docker ps -a --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}"
        echo ""
        echo "=== Docker Volumes ==="
        docker volume ls
        echo ""
        echo "=== Docker Networks ==="
        docker network ls
    } > "$docker_backup"
    
    log_info "Backup Docker состояния создан"
}

# Создание метаданных backup
create_backup_metadata() {
    log_info "Создание метаданных backup..."
    
    local metadata_file="$TEMP_DIR/backup_metadata.json"
    
    cat > "$metadata_file" << EOF
{
  "backup_timestamp": "$TIMESTAMP",
  "backup_date": "$(date -Iseconds)",
  "hostname": "$(hostname)",
  "backup_type": "full",
  "version": "1.0.0",
  "files": {
EOF

    local first=true
    for file in "$TEMP_DIR"/*.tar.gz "$TEMP_DIR"/*.txt; do
        if [[ -f "$file" ]]; then
            if [[ "$first" == "true" ]]; then
                first=false
            else
                echo "," >> "$metadata_file"
            fi
            local filename=$(basename "$file")
            local size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file")
            echo "    \"$filename\": { \"size\": $size, \"checksum\": \"$(sha256sum "$file" | cut -d' ' -f1)\" }" >> "$metadata_file"
        fi
    done

    cat >> "$metadata_file" << EOF
  },
  "system_info": {
    "os": "$(uname -s)",
    "arch": "$(uname -m)",
    "docker_version": "$(docker --version)",
    "disk_usage": {
      "archives": "$(du -sh /opt/telegram-archiver/archives 2>/dev/null | cut -f1 || echo 'N/A')",
      "media": "$(du -sh /opt/telegram-archiver/media 2>/dev/null | cut -f1 || echo 'N/A')",
      "data": "$(du -sh /opt/telegram-archiver/data 2>/dev/null | cut -f1 || echo 'N/A')"
    }
  }
}
EOF

    log_info "Метаданные backup созданы"
}

# Объединение всех backup файлов
create_final_backup() {
    log_info "Создание итогового backup архива..."
    
    local final_backup="$BACKUP_DIR/telegram_archiver_backup_$TIMESTAMP.tar.gz"
    
    tar -czf "$final_backup" -C "$TEMP_DIR" .
    
    local size=$(du -h "$final_backup" | cut -f1)
    log_info "Итоговый backup создан: $final_backup ($size)"
    
    # Создание symlink на последний backup
    local latest_link="$BACKUP_DIR/latest_backup.tar.gz"
    ln -sf "$(basename "$final_backup")" "$latest_link"
    
    echo "$final_backup"
}

# Очистка старых backup
cleanup_old_backups() {
    log_info "Очистка старых backup (старше $RETENTION_DAYS дней)..."
    
    local deleted_count=0
    while IFS= read -r -d '' file; do
        rm -f "$file"
        ((deleted_count++))
        log_info "Удален старый backup: $(basename "$file")"
    done < <(find "$BACKUP_DIR" -name "telegram_archiver_backup_*.tar.gz" -type f -mtime +$RETENTION_DAYS -print0)
    
    log_info "Удалено старых backup: $deleted_count"
}

# Проверка backup
verify_backup() {
    local backup_file="$1"
    
    log_info "Проверка целостности backup..."
    
    if tar -tzf "$backup_file" >/dev/null 2>&1; then
        log_info "Backup прошел проверку целостности"
        return 0
    else
        log_error "Backup не прошел проверку целостности"
        return 1
    fi
}

# Основная функция
main() {
    log_info "Запуск backup процесса для Telegram Chat Archiver"
    
    # Проверки
    check_dependencies
    create_backup_dir
    
    # Создание временной директории
    TEMP_DIR=$(mktemp -d)
    log_info "Использование временной директории: $TEMP_DIR"
    
    # Создание backup компонентов
    backup_archives
    backup_media
    backup_data
    backup_config
    backup_docker_state
    create_backup_metadata
    
    # Создание итогового backup
    local backup_file
    backup_file=$(create_final_backup)
    
    # Проверка backup
    if verify_backup "$backup_file"; then
        log_info "Backup успешно создан и проверен: $backup_file"
    else
        log_error "Ошибка при создании backup"
        exit 1
    fi
    
    # Очистка старых backup
    cleanup_old_backups
    
    log_info "Backup процесс завершен успешно"
}

# Проверка аргументов командной строки
case "${1:-}" in
    --help|-h)
        echo "Telegram Chat Archiver - Backup Script"
        echo ""
        echo "Использование: $0 [опции]"
        echo ""
        echo "Опции:"
        echo "  --help, -h           Показать эту справку"
        echo "  --verify FILE        Проверить целостность backup файла"
        echo "  --list               Показать список backup файлов"
        echo ""
        echo "Переменные окружения:"
        echo "  BACKUP_DIR           Директория для backup (по умолчанию: /opt/telegram-archiver/backups)"
        echo "  RETENTION_DAYS       Количество дней хранения backup (по умолчанию: 30)"
        exit 0
        ;;
    --verify)
        if [[ -z "${2:-}" ]]; then
            log_error "Укажите файл для проверки"
            exit 1
        fi
        verify_backup "$2"
        exit $?
        ;;
    --list)
        echo "Список backup файлов в $BACKUP_DIR:"
        ls -lah "$BACKUP_DIR"/telegram_archiver_backup_*.tar.gz 2>/dev/null || echo "Backup файлы не найдены"
        exit 0
        ;;
    "")
        # Запуск основной функции
        main
        ;;
    *)
        log_error "Неизвестная опция: $1"
        echo "Используйте --help для справки"
        exit 1
        ;;
esac