/**
 * Telegram Chat Archiver Dashboard JavaScript
 * Управляет интерфейсом панели мониторинга
 */

class TelegramArchiverDashboard {
    constructor() {
        this.messagesChart = null;
        this.messageTypesChart = null;
        this.updateInterval = 30000; // 30 секунд
        this.activityLogMaxItems = 50;
        this.apiAvailable = true;
        
        this.init();
    }

    /**
     * Инициализация dashboard
     */
    init() {
        this.addActivityLogItem('info', 'Dashboard инициализируется...', new Date());
        
        try {
            this.initializeCharts();
            this.loadInitialData();
            this.startAutoUpdate();
            this.setupEventListeners();
            
            this.addActivityLogItem('success', 'Dashboard успешно инициализирован', new Date());
        } catch (error) {
            console.error('Ошибка инициализации dashboard:', error);
            this.addActivityLogItem('error', 'Ошибка инициализации dashboard', new Date());
        }
    }

    /**
     * Инициализация графиков
     */
    initializeCharts() {
        try {
            // График сообщений во времени
            const messagesCtx = document.getElementById('messagesChart').getContext('2d');
            this.messagesChart = new Chart(messagesCtx, {
                type: 'line',
                data: {
                    labels: [],
                    datasets: [{
                        label: 'Messages Processed',
                        data: [],
                        borderColor: '#0088cc',
                        backgroundColor: 'rgba(0, 136, 204, 0.1)',
                        tension: 0.4,
                        fill: true
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                precision: 0
                            }
                        }
                    },
                    plugins: {
                        legend: {
                            display: false
                        }
                    }
                }
            });

            // График типов сообщений
            const messageTypesCtx = document.getElementById('messageTypesChart').getContext('2d');
            this.messageTypesChart = new Chart(messageTypesCtx, {
                type: 'doughnut',
                data: {
                    labels: ['Text', 'Photo', 'Video', 'Document', 'Voice', 'Other'],
                    datasets: [{
                        data: [0, 0, 0, 0, 0, 0],
                        backgroundColor: [
                            '#28a745',
                            '#17a2b8', 
                            '#ffc107',
                            '#dc3545',
                            '#6f42c1',
                            '#6c757d'
                        ]
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    }
                }
            });
            
            this.addActivityLogItem('success', 'Графики инициализированы', new Date());
        } catch (error) {
            console.error('Ошибка инициализации графиков:', error);
            this.addActivityLogItem('error', 'Ошибка инициализации графиков', new Date());
        }
    }

    /**
     * Загрузка начальных данных
     */
    async loadInitialData() {
        try {
            // Сначала проверим доступность API
            const pingResponse = await this.safeFetch('/api/monitoring/ping');
            if (pingResponse.ok) {
                this.apiAvailable = true;
                this.addActivityLogItem('success', 'API доступно', new Date());
                
                await Promise.all([
                    this.updateSystemStatus(),
                    this.updateHealthChecks(),
                    this.updateSystemInfo(),
                    this.updateStatistics()
                ]);
            } else {
                throw new Error('API недоступно');
            }
        } catch (error) {
            console.error('Ошибка загрузки данных:', error);
            this.apiAvailable = false;
            this.showOfflineMode();
            this.addActivityLogItem('warning', 'Переход в офлайн режим - API недоступно', new Date());
        }
    }

    /**
     * Безопасный fetch с таймаутом
     */
    async safeFetch(url, timeout = 5000) {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), timeout);
        
        try {
            const response = await fetch(url, {
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            return response;
        } catch (error) {
            clearTimeout(timeoutId);
            throw error;
        }
    }

    /**
     * Показать режим офлайн
     */
    showOfflineMode() {
        document.getElementById('system-status').textContent = 'Offline';
        document.getElementById('system-status').closest('.card').className = 'card text-white bg-secondary';
        
        // Показываем базовую информацию
        this.updateSystemInfoOffline();
        this.updateHealthChecksOffline();
        
        // Добавляем тестовые данные в графики
        this.addMessageDataPoint(0);
    }

    /**
     * Обновление информации о системе в офлайн режиме
     */
    updateSystemInfoOffline() {
        const systemInfoContainer = document.getElementById('system-info');
        systemInfoContainer.innerHTML = '';
        
        const infoItems = [
            { label: 'Status', value: 'Offline Mode' },
            { label: 'Version', value: '1.0.0' },
            { label: 'Framework', value: '.NET 8.0' },
            { label: 'Environment', value: 'Development' },
            { label: 'Mode', value: 'Standalone Dashboard' }
        ];
        
        infoItems.forEach(item => {
            const infoItem = document.createElement('div');
            infoItem.className = 'system-info-item';
            infoItem.innerHTML = `
                <div class="system-info-label">${item.label}:</div>
                <div class="system-info-value">${item.value}</div>
            `;
            systemInfoContainer.appendChild(infoItem);
        });
    }

    /**
     * Обновление health checks в офлайн режиме
     */
    updateHealthChecksOffline() {
        const healthChecksContainer = document.getElementById('health-checks');
        healthChecksContainer.innerHTML = '';
        
        const checks = [
            { name: 'Frontend', status: 'healthy' },
            { name: 'API Backend', status: 'unhealthy' },
            { name: 'Dashboard', status: 'healthy' }
        ];
        
        checks.forEach(check => {
            const healthItem = document.createElement('div');
            healthItem.className = `health-check-item ${check.status}`;
            
            healthItem.innerHTML = `
                <div class="health-check-name">${check.name}</div>
                <div class="health-check-status status-${check.status}>
                    ${check.status}
                </div>
            `;
            
            healthChecksContainer.appendChild(healthItem);
        });
        
        document.getElementById('health-last-updated').textContent = new Date().toLocaleString();
    }

    /**
     * Обновление статуса системы
     */
    async updateSystemStatus() {
        try {
            const response = await this.safeFetch('/api/monitoring/status');
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            
            // Обновляем статус системы
            const statusElement = document.getElementById('system-status');
            const statusCard = statusElement.closest('.card');
            
            statusElement.textContent = data.status;
            
            // Изменяем цвет карточки в зависимости от статуса
            statusCard.className = statusCard.className.replace(/bg-\w+/, '');
            switch(data.status.toLowerCase()) {
                case 'healthy':
                    statusCard.classList.add('bg-success');
                    break;
                case 'degraded':
                    statusCard.classList.add('bg-warning');
                    break;
                case 'unhealthy':
                    statusCard.classList.add('bg-danger');
                    break;
                default:
                    statusCard.classList.add('bg-secondary');
            }
            
            document.getElementById('last-check-time').textContent = new Date().toLocaleString();
            
        } catch (error) {
            console.error('Ошибка обновления статуса:', error);
            document.getElementById('system-status').textContent = 'Error';
            this.addActivityLogItem('error', 'Ошибка получения статуса системы', new Date());
        }
    }

    /**
     * Обновление health checks
     */
    async updateHealthChecks() {
        try {
            const response = await this.safeFetch('/api/monitoring/status');
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            
            const healthChecksContainer = document.getElementById('health-checks');
            healthChecksContainer.innerHTML = '';
            
            Object.entries(data.results).forEach(([name, result]) => {
                const healthItem = document.createElement('div');
                healthItem.className = `health-check-item ${result.status.toLowerCase()}`;
                
                healthItem.innerHTML = `
                    <div class="health-check-name">${this.formatHealthCheckName(name)}</div>
                    <div class="health-check-status status-${result.status.toLowerCase()}">
                        ${result.status}
                    </div>
                `;
                
                healthChecksContainer.appendChild(healthItem);
            });
            
            document.getElementById('health-last-updated').textContent = new Date().toLocaleString();
            
        } catch (error) {
            console.error('Ошибка обновления health checks:', error);
            this.updateHealthChecksOffline();
        }
    }

    /**
     * Обновление информации о системе
     */
    async updateSystemInfo() {
        try {
            const response = await this.safeFetch('/api/monitoring/info');
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            
            const systemInfoContainer = document.getElementById('system-info');
            systemInfoContainer.innerHTML = '';
            
            const infoItems = [
                { label: 'Version', value: data.version },
                { label: 'Framework', value: data.framework },
                { label: 'Environment', value: data.environment },
                { label: 'Machine', value: data.machineName },
                { label: 'Process ID', value: data.processId },
                { label: 'Uptime', value: this.formatUptime(data.uptime) },
                { label: 'Memory', value: this.formatBytes(data.workingSet) }
            ];
            
            infoItems.forEach(item => {
                const infoItem = document.createElement('div');
                infoItem.className = 'system-info-item';
                infoItem.innerHTML = `
                    <div class="system-info-label">${item.label}:</div>
                    <div class="system-info-value">${item.value}</div>
                `;
                systemInfoContainer.appendChild(infoItem);
            });
            
            // Обновляем версию в футере
            document.getElementById('app-version').textContent = data.version;
            
        } catch (error) {
            console.error('Ошибка обновления системной информации:', error);
            this.updateSystemInfoOffline();
        }
    }

    /**
     * Обновление статистики
     */
    async updateStatistics() {
        try {
            const response = await this.safeFetch('/api/monitoring/statistics');
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            
            // Обновляем счетчики
            let totalMessages = 0;
            const messageTypes = data.messageStatistics || {};
            
            Object.values(messageTypes).forEach(count => {
                totalMessages += count;
            });
            
            document.getElementById('messages-count').textContent = totalMessages.toLocaleString();
            
            // Обновляем использование памяти
            this.updateMemoryUsage();
            
            // Обновляем график типов сообщений
            this.updateMessageTypesChart(messageTypes);
            
            // Добавляем точку в график сообщений
            this.addMessageDataPoint(totalMessages);
            
        } catch (error) {
            console.error('Ошибка обновления статистики:', error);
            // В офлайн режиме показываем нули
            document.getElementById('messages-count').textContent = '0';
        }
    }

    /**
     * Обновление использования памяти
     */
    async updateMemoryUsage() {
        try {
            const response = await this.safeFetch('/api/monitoring/info');
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            
            const data = await response.json();
            
            const memoryBytes = data.workingSet || 0;
            const memoryMB = Math.round(memoryBytes / (1024 * 1024));
            document.getElementById('memory-usage').textContent = `${memoryMB} MB`;
        } catch (error) {
            console.error('Ошибка обновления памяти:', error);
            document.getElementById('memory-usage').textContent = '0 MB';
        }
    }

    /**
     * Обновление графика типов сообщений
     */
    updateMessageTypesChart(messageTypes) {
        const chartData = [
            messageTypes.Text || 0,
            messageTypes.Photo || 0,
            messageTypes.Video || 0,
            messageTypes.Document || 0,
            messageTypes.Voice || 0,
            messageTypes.Other || 0
        ];
        
        this.messageTypesChart.data.datasets[0].data = chartData;
        this.messageTypesChart.update();
    }

    /**
     * Добавление точки данных в график сообщений
     */
    addMessageDataPoint(messageCount) {
        const now = new Date().toLocaleTimeString();
        
        // Добавляем новую точку
        this.messagesChart.data.labels.push(now);
        this.messagesChart.data.datasets[0].data.push(messageCount);
        
        // Ограничиваем количество точек (последние 20)
        if (this.messagesChart.data.labels.length > 20) {
            this.messagesChart.data.labels.shift();
            this.messagesChart.data.datasets[0].data.shift();
        }
        
        this.messagesChart.update();
    }

    /**
     * Добавление элемента в лог активности
     */
    addActivityLogItem(type, message, timestamp) {
        const activityContainer = document.getElementById('activity-log');
        
        // Удаляем placeholder текст при первом добавлении
        if (activityContainer.children.length === 1 && 
            activityContainer.firstElementChild.classList.contains('text-center')) {
            activityContainer.innerHTML = '';
        }
        
        const activityItem = document.createElement('div');
        activityItem.className = 'activity-item fade-in';
        
        const iconClass = this.getActivityIconClass(type);
        
        activityItem.innerHTML = `
            <div class="activity-icon ${type}">
                <i class="${iconClass}"></i>
            </div>
            <div class="activity-content">
                <div class="activity-message">${message}</div>
                <div class="activity-time">${timestamp.toLocaleString()}</div>
            </div>
        `;
        
        // Добавляем в начало
        activityContainer.insertBefore(activityItem, activityContainer.firstChild);
        
        // Ограничиваем количество элементов
        while (activityContainer.children.length > this.activityLogMaxItems) {
            activityContainer.removeChild(activityContainer.lastChild);
        }
    }

    /**
     * Получение класса иконки для типа активности
     */
    getActivityIconClass(type) {
        const iconMap = {
            'info': 'fas fa-info-circle',
            'success': 'fas fa-check-circle',
            'warning': 'fas fa-exclamation-triangle',
            'error': 'fas fa-times-circle'
        };
        return iconMap[type] || 'fas fa-circle';
    }

    /**
     * Форматирование названия health check
     */
    formatHealthCheckName(name) {
        return name.charAt(0).toUpperCase() + name.slice(1).replace(/([A-Z])/g, ' $1');
    }

    /**
     * Форматирование времени работы
     */
    formatUptime(uptimeString) {
        try {
            // Парсим строку времени работы (например, "1.12:34:56.789")
            if (typeof uptimeString === 'string') {
                const parts = uptimeString.split(':');
                if (parts.length >= 3) {
                    const hours = parseInt(parts[0].split('.').pop() || '0');
                    const minutes = parseInt(parts[1]);
                    const days = Math.floor(hours / 24);
                    const remainingHours = hours % 24;
                    
                    if (days > 0) {
                        return `${days}d ${remainingHours}h ${minutes}m`;
                    } else if (remainingHours > 0) {
                        return `${remainingHours}h ${minutes}m`;
                    } else {
                        return `${minutes}m`;
                    }
                }
            }
        } catch (error) {
            console.error('Ошибка форматирования uptime:', error);
        }
        return uptimeString || 'Unknown';
    }

    /**
     * Форматирование байтов в человекочитаемый формат
     */
    formatBytes(bytes) {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    /**
     * Запуск автоматического обновления
     */
    startAutoUpdate() {
        setInterval(async () => {
            if (this.apiAvailable) {
                try {
                    await this.updateSystemStatus();
                    await this.updateHealthChecks();
                    await this.updateStatistics();
                    
                    this.addActivityLogItem('info', 'Данные обновлены', new Date());
                } catch (error) {
                    console.error('Ошибка автообновления:', error);
                    this.addActivityLogItem('error', 'Ошибка автообновления данных', new Date());
                }
            } else {
                // Периодически проверяем доступность API
                try {
                    const response = await this.safeFetch('/api/monitoring/ping');
                    if (response.ok) {
                        this.apiAvailable = true;
                        this.addActivityLogItem('success', 'API снова доступно', new Date());
                        await this.loadInitialData();
                    }
                } catch (error) {
                    // API все еще недоступно
                }
            }
        }, this.updateInterval);
    }

    /**
     * Настройка обработчиков событий
     */
    setupEventListeners() {
        // Обработка переключения темы
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addListener(() => {
                this.updateChartsTheme();
            });
        }
        
        // Обработка изменения размера окна
        window.addEventListener('resize', () => {
            if (this.messagesChart) this.messagesChart.resize();
            if (this.messageTypesChart) this.messageTypesChart.resize();
        });
        
        // Обработка видимости страницы
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.addActivityLogItem('info', 'Dashboard приостановлен (вкладка неактивна)', new Date());
            } else {
                this.addActivityLogItem('info', 'Dashboard возобновлен (вкладка активна)', new Date());
                // Немедленное обновление при возврате на вкладку
                if (this.apiAvailable) {
                    this.updateSystemStatus();
                }
            }
        });
    }

    /**
     * Обновление темы графиков
     */
    updateChartsTheme() {
        const isDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const textColor = isDark ? '#ffffff' : '#333333';
        const gridColor = isDark ? '#444444' : '#e0e0e0';
        
        [this.messagesChart, this.messageTypesChart].forEach(chart => {
            if (chart && chart.options) {
                chart.options.plugins.legend.labels.color = textColor;
                if (chart.options.scales) {
                    Object.values(chart.options.scales).forEach(scale => {
                        scale.ticks.color = textColor;
                        scale.grid.color = gridColor;
                    });
                }
                chart.update();
            }
        });
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new TelegramArchiverDashboard();
});