namespace Telegram.HostedApp.Services;

/// <summary>
/// Интерфейс сервиса для работы с Telegram Bot API
/// </summary>
public interface ITelegramBotService
{
	/// <summary>
	/// Отправить сообщение администратору
	/// </summary>
	/// <param name="message">Текст сообщения</param>
	/// <param name="cancellationToken">Токен отмены</param>
	Task SendAdminMessageAsync(string message, CancellationToken cancellationToken = default);

	/// <summary>
	/// Отправить уведомление об ошибке
	/// </summary>
	/// <param name="error">Сообщение об ошибке</param>
	/// <param name="exception">Исключение, если есть</param>
	/// <param name="cancellationToken">Токен отмены</param>
	Task SendErrorNotificationAsync(string error, Exception? exception = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Отправить отчет о статистике
	/// </summary>
	/// <param name="statistics">Статистика для отчета</param>
	/// <param name="cancellationToken">Токен отмены</param>
	Task SendStatisticsReportAsync(object statistics, CancellationToken cancellationToken = default);

	/// <summary>
	/// Обработать команду управления
	/// </summary>
	/// <param name="command">Команда</param>
	/// <param name="userId">ID пользователя, отправившего команду</param>
	/// <param name="cancellationToken">Токен отмены</param>
	Task<string> ProcessManagementCommandAsync(string command, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Проверить доступность бота
	/// </summary>
	/// <returns>True, если бот доступен</returns>
	Task<bool> IsBotAvailableAsync();

	/// <summary>
	/// Запустить прослушивание команд
	/// </summary>
	/// <param name="cancellationToken">Токен отмены</param>
	Task StartListeningAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Остановить прослушивание команд
	/// </summary>
	Task StopListeningAsync();
	
	/// <summary>
	/// Установить провайдер статуса системы
	/// </summary>
	/// <param name="statusProvider">Провайдер статуса</param>
	void SetStatusProvider(ISystemStatusProvider statusProvider);
}

/// <summary>
/// Интерфейс провайдера статуса системы
/// </summary>
public interface ISystemStatusProvider
{
	/// <summary>
	/// Получить статус подключения к Telegram
	/// </summary>
	/// <returns>True, если подключение активно</returns>
	Task<bool> IsTelegramConnectedAsync();
	
	/// <summary>
	/// Получить статистику системы
	/// </summary>
	/// <returns>Статистика в виде строки</returns>
	Task<string> GetSystemStatisticsAsync();
}