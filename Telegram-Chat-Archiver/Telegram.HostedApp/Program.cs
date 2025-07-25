using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Serilog;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Services;
using Telegram.HostedApp.HealthChecks;

namespace Telegram.HostedApp;

internal class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("=== Запуск Telegram Chat Archiver (Режим реального времени) ===");
		Console.WriteLine("Изменения: переход с периодической перезаписи на инкрементальное добавление сообщений");
		Console.WriteLine("- Используется MarkdownArchiver для потокобезопасной записи");
		Console.WriteLine("- TelegramPollingService заменяет TelegramArchiverBackgroundService");
		Console.WriteLine("- Интервал проверки уменьшен до 30 секунд");
		Console.WriteLine("- Поддержка переменных окружения для конфигурации");
		Console.WriteLine();
		
		// Проверяем аргументы командной строки для health check
		if (args.Contains("--health-check"))
		{
			Environment.Exit(await PerformHealthCheckAsync() ? 0 : 1);
		}

		try
		{
			Console.WriteLine("Загрузка конфигурации...");
			
			// Создание конфигурации с приоритетом переменных окружения
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
				.AddEnvironmentVariables() // Переменные окружения имеют приоритет
				.Build();

			Console.WriteLine("Настройка логирования...");
			
			// Логируем значения ключевых переменных окружения для отладки
			LogEnvironmentVariables();
			
			// Настройка Serilog
			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(configuration)
				.CreateLogger();

			Console.WriteLine("Веб-интерфейс будет доступен по адресу: http://localhost:5000");
			Console.WriteLine("Для остановки нажмите Ctrl+C");
			
			Log.Information("Запуск Telegram Chat Archiver с расширенной функциональностью");
			Log.Information("Веб-интерфейс будет доступен по адресу: http://localhost:5000");

			Console.WriteLine("Создание и настройка веб-хоста...");
			
			// Создание и запуск хоста
			var host = CreateHostBuilder(args, configuration).Build();
			
			Console.WriteLine("Запускаем веб-сервер...");
			Console.WriteLine("Если сервер не запускается, проверьте что порт 5000 свободен");
			
			await host.RunAsync();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
			Console.WriteLine($"Тип исключения: {ex.GetType().Name}");
			Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
			
			if (ex.InnerException != null)
			{
				Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
			}
			
			Log.Fatal(ex, "Критическая ошибка при запуске приложения");
			Console.WriteLine("Нажмите любую клавишу для выхода...");
			Console.ReadKey();
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	/// <summary>
	/// Выполнение быстрой проверки состояния для Docker health check
	/// </summary>
	/// <returns>True если система здорова, иначе false</returns>
	static async Task<bool> PerformHealthCheckAsync()
	{
		try
		{
			// Базовые проверки для быстрого health check
			var archivesPath = Environment.GetEnvironmentVariable("ArchiveConfig__OutputPath") ?? "archives";
			var mediaPath = Environment.GetEnvironmentVariable("ArchiveConfig__MediaPath") ?? "media";

			// Создаем директории если их нет
			Directory.CreateDirectory(archivesPath);
			Directory.CreateDirectory(mediaPath);

			// Проверяем возможность записи
			var testFile = Path.Combine(archivesPath, $"health_test_{Guid.NewGuid()}.tmp");
			await File.WriteAllTextAsync(testFile, "test");
			File.Delete(testFile);

			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Создание хоста приложения
	/// </summary>
	/// <param name="args">Аргументы командной строки</param>
	/// <param name="configuration">Конфигурация</param>
	/// <returns>Построитель хоста</returns>
	static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
		Host.CreateDefaultBuilder(args)
			.UseSerilog()
			.ConfigureWebHostDefaults(webBuilder =>
			{
				Console.WriteLine("Настройка веб-хоста...");
				
				// Настройка URL - убираем HTTPS для упрощения
				webBuilder.UseUrls("http://localhost:5000");
				
				webBuilder.Configure(app =>
				{
					Console.WriteLine("Настройка middleware pipeline...");
					
					var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
					
					if (env.IsDevelopment())
					{
						Console.WriteLine("Включение developer exception page...");
						app.UseDeveloperExceptionPage();
					}

					Console.WriteLine("Настройка статических файлов...");
					
					// Статические файлы
					app.UseDefaultFiles();
					app.UseStaticFiles();

					Console.WriteLine("Настройка health checks...");
					
					// Health check endpoints  
					app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
					{
						ResponseWriter = WriteHealthCheckResponse
					});

					Console.WriteLine("Настройка маршрутизации...");
					
					// Маршрутизация
					app.UseRouting();
					app.UseEndpoints(endpoints =>
					{
						// Простой тестовый endpoint
						endpoints.MapGet("/test", async context =>
						{
							await context.Response.WriteAsync("Веб-сервер работает!");
						});
						
						endpoints.MapControllers();
						endpoints.MapFallbackToFile("index.html");
					});
					
					Console.WriteLine("Middleware pipeline настроен успешно");
				});

				webBuilder.ConfigureServices(services =>
				{
					Console.WriteLine("Регистрация базовых сервисов...");
					
					// ASP.NET Core services
					services.AddControllers();

					// Минимальные health checks
					services.AddHealthChecks()
						.AddCheck("basic", () => HealthCheckResult.Healthy("Веб-сервер работает"), 
							tags: new[] { "live", "ready", "startup" });
						
					Console.WriteLine("Базовые сервисы зарегистрированы");
				});
			})
			.ConfigureServices((context, services) =>
			{
				Console.WriteLine("Регистрация дополнительных сервисов...");
				
				try
				{
					// Регистрация конфигурации
					services.Configure<TelegramConfig>(configuration.GetSection("TelegramConfig"));
					services.Configure<BotConfig>(configuration.GetSection("BotConfig"));
					services.Configure<ArchiveConfig>(configuration.GetSection("ArchiveConfig"));

					Console.WriteLine("Конфигурация зарегистрирована");

					// Регистрация сервисов по одному с проверками
					RegisterServicesWithErrorHandling(services);
					
					Console.WriteLine("Все сервисы обработаны");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"ОШИБКА при регистрации сервисов: {ex.Message}");
					Log.Error(ex, "Ошибка при регистрации сервисов, но веб-сервер будет запущен");
					// Не бросаем исключение - веб-сервер должен работать
				}
			});

	/// <summary>
	/// Регистрация сервисов с обработкой ошибок
	/// </summary>
	private static void RegisterServicesWithErrorHandling(IServiceCollection services)
	{
		try
		{
			Console.WriteLine("Регистрация MarkdownArchiver...");
			services.AddSingleton<MarkdownArchiver>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации MarkdownArchiver: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация IMarkdownService...");
			services.AddSingleton<IMarkdownService, MarkdownService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации IMarkdownService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация IMediaDownloadService...");
			services.AddSingleton<IMediaDownloadService, MediaDownloadService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации IMediaDownloadService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация ITelegramNotificationService...");
			services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации ITelegramNotificationService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация ITelegramBotService...");
			services.AddSingleton<ITelegramBotService, TelegramBotService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации ITelegramBotService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация ISyncStateService...");
			services.AddSingleton<ISyncStateService, SyncStateService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации ISyncStateService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация IStatisticsService...");
			services.AddSingleton<IStatisticsService, StatisticsService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации IStatisticsService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация IRetryService...");
			services.AddSingleton<IRetryService, RetryService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации IRetryService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация ITelegramArchiverService...");
			services.AddSingleton<ITelegramArchiverService, TelegramArchiverService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации ITelegramArchiverService: {ex.Message}");
		}

		try
		{
			Console.WriteLine("Регистрация TelegramPollingService как HostedService...");
			// Используем новый polling сервис для режима реального времени
			services.AddHostedService<TelegramPollingService>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка регистрации TelegramPollingService: {ex.Message}");
		}

		Console.WriteLine("Регистрация сервисов завершена");
	}

	/// <summary>
	/// Кастомная функция для записи ответа health check
	/// </summary>
	static async Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)
	{
		context.Response.ContentType = "application/json; charset=utf-8";

		var response = new
		{
			status = healthReport.Status.ToString(),
			totalDuration = healthReport.TotalDuration.TotalMilliseconds,
			results = healthReport.Entries.ToDictionary(
				kvp => kvp.Key,
				kvp => new
				{
					status = kvp.Value.Status.ToString(),
					description = kvp.Value.Description,
					duration = kvp.Value.Duration.TotalMilliseconds,
					data = kvp.Value.Data
				}
			),
			timestamp = DateTime.UtcNow
		};

		var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		});

		await context.Response.WriteAsync(jsonResponse);
	}

	/// <summary>
	/// Логирование ключевых переменных окружения для отладки
	/// </summary>
	private static void LogEnvironmentVariables()
	{
		Console.WriteLine("=== Проверка переменных окружения ===");
		
		var envVars = new[]
		{
			"TelegramConfig__ApiId",
			"TelegramConfig__ApiHash", 
			"TelegramConfig__PhoneNumber",
			"TelegramConfig__SessionFile",
			"ArchiveConfig__OutputPath",
			"ArchiveConfig__MediaPath",
			"ArchiveConfig__TargetChat",
			"BotConfig__BotToken",
			"BotConfig__EnableManagementCommands"
		};

		foreach (var envVar in envVars)
		{
			var value = Environment.GetEnvironmentVariable(envVar);
			if (!string.IsNullOrEmpty(value))
			{
				// Маскируем чувствительные данные
				if (envVar.Contains("Token") || envVar.Contains("Hash") || envVar.Contains("Phone"))
				{
					Console.WriteLine($"{envVar} = ***скрыто***");
				}
				else
				{
					Console.WriteLine($"{envVar} = {value}");
				}
			}
			else
			{
				Console.WriteLine($"{envVar} = <не задано>");
			}
		}
		
		Console.WriteLine("=====================================");
	}
}
