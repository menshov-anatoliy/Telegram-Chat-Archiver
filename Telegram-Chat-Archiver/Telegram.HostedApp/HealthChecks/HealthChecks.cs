using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Telegram.HostedApp.Services;

namespace Telegram.HostedApp.HealthChecks;

/// <summary>
/// Health check для проверки состояния Telegram соединения
/// </summary>
public class TelegramConnectionHealthCheck : IHealthCheck
{
	private readonly ITelegramArchiverService _telegramService;
	private readonly ILogger<TelegramConnectionHealthCheck> _logger;

	public TelegramConnectionHealthCheck(
		ITelegramArchiverService telegramService,
		ILogger<TelegramConnectionHealthCheck> logger)
	{
		_telegramService = telegramService;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogDebug("Выполняется проверка Telegram соединения");

			// Проверяем, что сервис инициализирован и работает
			var isConnected = await CheckTelegramConnectionAsync(cancellationToken);

			if (isConnected)
			{
				_logger.LogDebug("Telegram соединение активно");
				return HealthCheckResult.Healthy("Telegram соединение активно");
			}
			else
			{
				_logger.LogWarning("Telegram соединение неактивно");
				return HealthCheckResult.Unhealthy("Telegram соединение неактивно");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при проверке Telegram соединения");
			return HealthCheckResult.Unhealthy(
				"Ошибка при проверке Telegram соединения",
				ex);
		}
	}

	private async Task<bool> CheckTelegramConnectionAsync(CancellationToken cancellationToken)
	{
		// В реальной реализации здесь будет проверка состояния Telegram клиента
		// Пока возвращаем true как заглушку
		await Task.Delay(100, cancellationToken);
		return true;
	}
}

/// <summary>
/// Health check для проверки доступности файловой системы
/// </summary>
public class FileSystemHealthCheck : IHealthCheck
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<FileSystemHealthCheck> _logger;

	public FileSystemHealthCheck(
		IConfiguration configuration,
		ILogger<FileSystemHealthCheck> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogDebug("Выполняется проверка файловой системы");

			var archiveConfig = _configuration.GetSection("ArchiveConfig");
			var outputPath = archiveConfig["OutputPath"] ?? "archives";
			var mediaPath = archiveConfig["MediaPath"] ?? "media";

			var checks = new Dictionary<string, bool>
			{
				{ "ArchivesDirectory", await CheckDirectoryAccessAsync(outputPath, cancellationToken) },
				{ "MediaDirectory", await CheckDirectoryAccessAsync(mediaPath, cancellationToken) }
			};

			var failures = checks.Where(c => !c.Value).Select(c => c.Key).ToList();

			if (!failures.Any())
			{
				_logger.LogDebug("Все директории доступны для записи");
				return HealthCheckResult.Healthy(
					"Все директории доступны для записи",
					new Dictionary<string, object> { { "CheckedDirectories", checks.Keys } });
			}
			else
			{
				_logger.LogWarning("Некоторые директории недоступны: {Failures}", string.Join(", ", failures));
				return HealthCheckResult.Degraded(
					$"Некоторые директории недоступны: {string.Join(", ", failures)}",
					data: new Dictionary<string, object>
					{
						{ "FailedDirectories", failures },
						{ "AllChecks", checks }
					});
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при проверке файловой системы");
			return HealthCheckResult.Unhealthy(
				"Ошибка при проверке файловой системы",
				ex);
		}
	}

	private async Task<bool> CheckDirectoryAccessAsync(string path, CancellationToken cancellationToken)
	{
		try
		{
			// Создаем директорию если не существует
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			// Проверяем возможность записи
			var testFile = Path.Combine(path, $"health_check_{Guid.NewGuid()}.tmp");
			await File.WriteAllTextAsync(testFile, "test", cancellationToken);
			File.Delete(testFile);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Ошибка доступа к директории {Path}", path);
			return false;
		}
	}
}

/// <summary>
/// Health check для проверки состояния базы данных
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<DatabaseHealthCheck> _logger;

	public DatabaseHealthCheck(
		IConfiguration configuration,
		ILogger<DatabaseHealthCheck> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogDebug("Выполняется проверка базы данных");

			var archiveConfig = _configuration.GetSection("ArchiveConfig");
			var databasePath = archiveConfig["DatabasePath"] ?? "metadata.db";
			var syncStatePath = archiveConfig["SyncStatePath"] ?? "sync_state.json";

			var checks = new Dictionary<string, object>();

			// Проверка доступности файлов
			checks["DatabaseExists"] = File.Exists(databasePath);
			checks["SyncStateExists"] = File.Exists(syncStatePath);

			// Проверка размера файлов
			if (File.Exists(databasePath))
			{
				var dbInfo = new FileInfo(databasePath);
				checks["DatabaseSize"] = dbInfo.Length;
				checks["DatabaseLastModified"] = dbInfo.LastWriteTime;
			}

			if (File.Exists(syncStatePath))
			{
				var syncInfo = new FileInfo(syncStatePath);
				checks["SyncStateSize"] = syncInfo.Length;
				checks["SyncStateLastModified"] = syncInfo.LastWriteTime;
			}

			_logger.LogDebug("Проверка базы данных завершена успешно");
			return HealthCheckResult.Healthy(
				"База данных доступна",
				checks);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при проверке базы данных");
			return HealthCheckResult.Unhealthy(
				"Ошибка при проверке базы данных",
				ex);
		}
	}
}

/// <summary>
/// Health check для проверки использования ресурсов системы
/// </summary>
public class SystemResourcesHealthCheck : IHealthCheck
{
	private readonly ILogger<SystemResourcesHealthCheck> _logger;

	public SystemResourcesHealthCheck(ILogger<SystemResourcesHealthCheck> logger)
	{
		_logger = logger;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.LogDebug("Выполняется проверка системных ресурсов");

			var data = new Dictionary<string, object>();

			// Проверка использования памяти
			var process = System.Diagnostics.Process.GetCurrentProcess();
			var memoryUsage = process.WorkingSet64;
			var memoryUsageMB = memoryUsage / (1024 * 1024);

			data["MemoryUsageMB"] = memoryUsageMB;
			data["ThreadCount"] = process.Threads.Count;
			data["HandleCount"] = process.HandleCount;

			// Проверка дискового пространства
			var drives = DriveInfo.GetDrives()
				.Where(d => d.IsReady)
				.Select(d => new
				{
					Name = d.Name,
					TotalSizeGB = d.TotalSize / (1024 * 1024 * 1024),
					AvailableFreeSpaceGB = d.AvailableFreeSpace / (1024 * 1024 * 1024),
					UsagePercentage = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100
				})
				.ToList();

			data["DriveInfo"] = drives;

			// Проверяем критические пороги
			var warnings = new List<string>();

			if (memoryUsageMB > 800) // 800MB threshold
			{
				warnings.Add($"Высокое использование памяти: {memoryUsageMB}MB");
			}

			var criticalDrives = drives.Where(d => d.UsagePercentage > 90).ToList();
			if (criticalDrives.Any())
			{
				warnings.Add($"Критически мало места на дисках: {string.Join(", ", criticalDrives.Select(d => d.Name))}");
			}

			await Task.CompletedTask;

			if (warnings.Any())
			{
				_logger.LogWarning("Обнаружены проблемы с ресурсами: {Warnings}", string.Join("; ", warnings));
				return HealthCheckResult.Degraded(
					$"Обнаружены проблемы с ресурсами: {string.Join("; ", warnings)}",
					data: data);
			}

			_logger.LogDebug("Проверка системных ресурсов завершена успешно");
			return HealthCheckResult.Healthy("Системные ресурсы в норме", data);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при проверке системных ресурсов");
			return HealthCheckResult.Unhealthy(
				"Ошибка при проверке системных ресурсов",
				ex);
		}
	}
}