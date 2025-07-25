using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.HostedApp.Configuration;
using Telegram.HostedApp.Models;
using WTelegram;
using TL;

namespace Telegram.HostedApp.Services;

/// <summary>
/// Реализация сервиса архивирования Telegram сообщений
/// </summary>
public class TelegramArchiverService : ITelegramArchiverService, IAsyncDisposable
{
	private readonly ILogger<TelegramArchiverService> _logger;
	private readonly TelegramConfig _config;
	private readonly ArchiveConfig _archiveConfig;
	private readonly IMarkdownService _markdownService;
	private readonly IMediaDownloadService _mediaDownloadService;
	private readonly ITelegramNotificationService _notificationService;
	private Client? _client;
	private Dictionary<long, ChatBase>? _allChats;
	private Dictionary<long, User>? _allUsers;
	private bool _disposed;

	public TelegramArchiverService(
		ILogger<TelegramArchiverService> logger,
		IOptions<TelegramConfig> config,
		IOptions<ArchiveConfig> archiveConfig,
		IMarkdownService markdownService,
		IMediaDownloadService mediaDownloadService,
		ITelegramNotificationService notificationService)
	{
		_logger = logger;
		_config = config.Value;
		_archiveConfig = archiveConfig.Value;
		_markdownService = markdownService;
		_mediaDownloadService = mediaDownloadService;
		_notificationService = notificationService;
	}

	/// <summary>
	/// Проверка подключения к Telegram API
	/// </summary>
	public async Task<bool> IsConnectedAsync()
	{
		try
		{
			await InitializeClientAsync();
			if (_client == null)
				return false;

			// Проверяем подключение через вызов GetMe
			var me = await _client.LoginUserIfNeeded();
			return me != null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при подключении к Telegram API");
			await _notificationService.SendErrorNotificationAsync("Ошибка подключения к Telegram API", ex);
			return false;
		}
	}

	/// <summary>
	/// Аутентификация пользователя в Telegram
	/// </summary>
	public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			await InitializeClientAsync();
			
			if (_client == null)
			{
				_logger.LogError("Клиент Telegram не инициализирован");
				return false;
			}

			_logger.LogInformation("Начинается аутентификация в Telegram API");
			
			// Выполняем аутентификацию
			var myself = await _client.LoginUserIfNeeded();
			
			if (myself == null)
			{
				_logger.LogError("Аутентификация не удалась");
				return false;
			}

			_logger.LogInformation("Аутентификация завершена успешно. Пользователь: {UserName} (@{Username})", 
				GetUserDisplayName(myself), myself.username ?? "без username");
			
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при аутентификации в Telegram API");
			await _notificationService.SendErrorNotificationAsync("Ошибка аутентификации в Telegram API", ex);
			return false;
		}
	}

	/// <summary>
	/// Получение списка доступных чатов
	/// </summary>
	public async Task<Dictionary<long, string>> GetChatsAsync()
	{
		try
		{
			await InitializeClientAsync();
			
			if (_client == null)
			{
				_logger.LogWarning("Клиент Telegram не инициализирован");
				return new Dictionary<long, string>();
			}

			_logger.LogInformation("Получение списка чатов из Telegram API");
			
			// Получаем диалоги
			var dialogs = await _client.Messages_GetAllDialogs();
			_allChats = dialogs.chats;
			_allUsers = dialogs.users;

			var chats = new Dictionary<long, string>();
			
			foreach (var dialog in dialogs.dialogs)
			{
				var peer = dialog.Peer;
				string chatTitle = string.Empty;
				long chatId = 0;

				// Определяем тип чата и получаем название
				if (peer is PeerUser peerUser)
				{
					chatId = peerUser.user_id;
					if (_allUsers.TryGetValue(peerUser.user_id, out User? user))
					{
						chatTitle = GetUserDisplayName(user);
					}
				}
				else if (peer is PeerChat peerChat)
				{
					chatId = peerChat.chat_id;
					if (_allChats.TryGetValue(peerChat.chat_id, out ChatBase? chat))
					{
						chatTitle = chat.Title;
					}
				}
				else if (peer is PeerChannel peerChannel)
				{
					chatId = peerChannel.channel_id;
					if (_allChats.TryGetValue(peerChannel.channel_id, out ChatBase? channel))
					{
						chatTitle = channel.Title;
					}
				}

				if (!string.IsNullOrEmpty(chatTitle) && chatId != 0)
				{
					chats[chatId] = chatTitle;
				}
			}
			
			_logger.LogInformation("Получено {ChatCount} чатов", chats.Count);
			return chats;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при получении списка чатов");
			await _notificationService.SendErrorNotificationAsync("Ошибка получения списка чатов", ex);
			return new Dictionary<long, string>();
		}
	}

	/// <summary>
	/// Поиск чата по имени или ID
	/// </summary>
	public async Task<(long ChatId, string ChatTitle)?> FindChatAsync(string chatIdentifier)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(chatIdentifier))
				return null;

			var chats = await GetChatsAsync();
			
			// Попытка найти по ID
			if (long.TryParse(chatIdentifier, out long chatId) && chats.ContainsKey(chatId))
			{
				return (chatId, chats[chatId]);
			}
			
			// Поиск по названию
			var chatByName = chats.FirstOrDefault(c => 
				c.Value.Equals(chatIdentifier, StringComparison.OrdinalIgnoreCase) ||
				c.Value.Contains(chatIdentifier, StringComparison.OrdinalIgnoreCase));
				
			if (chatByName.Key != 0)
			{
				return (chatByName.Key, chatByName.Value);
			}
			
			_logger.LogWarning("Чат не найден: {ChatIdentifier}", chatIdentifier);
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при поиске чата {ChatIdentifier}", chatIdentifier);
			await _notificationService.SendErrorNotificationAsync($"Ошибка поиска чата: {chatIdentifier}", ex);
			return null;
		}
	}

	/// <summary>
	/// Получение сообщений из указанного чата
	/// </summary>
	public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(long chatId, int limit = 100, int offsetId = 0, CancellationToken cancellationToken = default)
	{
		try
		{
			await InitializeClientAsync();
			
			if (_client == null)
			{
				_logger.LogWarning("Клиент Telegram не инициализирован");
				return Enumerable.Empty<ChatMessage>();
			}

			_logger.LogInformation("Получение сообщений из чата {ChatId} через Telegram API", chatId);
			
			// Определяем InputPeer для чата
			InputPeer inputPeer = await GetInputPeerFromChatId(chatId);
			
			// Получаем историю сообщений
			var history = await _client.Messages_GetHistory(
				peer: inputPeer,
				limit: limit,
				offset_id: offsetId);

			var messages = new List<ChatMessage>();
			
			foreach (var message in history.Messages)
			{
				cancellationToken.ThrowIfCancellationRequested();
				
				var chatMessage = ConvertTelegramMessageToChatMessage(message);
				if (chatMessage != null)
				{
					messages.Add(chatMessage);
				}
			}
			
			// Сортируем сообщения по дате (старые сначала)
			messages = messages.OrderBy(m => m.Date).ToList();
			
			_logger.LogInformation("Получено {MessageCount} сообщений из чата {ChatId}", messages.Count, chatId);
			return messages;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при получении сообщений из чата {ChatId}", chatId);
			await _notificationService.SendErrorNotificationAsync($"Ошибка получения сообщений из чата {chatId}", ex, cancellationToken);
			return [];
		}
	}

	/// <summary>
	/// Архивирование сообщений из указанного чата
	/// </summary>
	public async Task ArchiveChatAsync(long chatId, CancellationToken cancellationToken = default)
	{
		try
		{
			var chats = await GetChatsAsync();
			if (!chats.TryGetValue(chatId, out string? chatTitle))
			{
				_logger.LogWarning("Чат {ChatId} не найден для архивирования", chatId);
				return;
			}

			_logger.LogInformation("Начинается архивирование чата {ChatId}: {ChatTitle}", chatId, chatTitle);

			var messages = await GetMessagesAsync(chatId, _archiveConfig.MaxMessagesPerFile, cancellationToken: cancellationToken);
			var messagesList = messages.ToList();
			
			if (!messagesList.Any())
			{
				_logger.LogInformation("Нет новых сообщений для архивирования в чате {ChatTitle}", chatTitle);
				return;
			}

			// Загружаем медиафайлы
			foreach (var message in messagesList.Where(m => m.Media != null))
			{
				if (cancellationToken.IsCancellationRequested)
					break;
					
				try
				{
					message.Media = await _mediaDownloadService.DownloadMediaAsync(
						message.Media!, chatTitle, message.Date, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Не удалось загрузить медиафайл для сообщения {MessageId}", message.Id);
				}
			}

			// Группируем сообщения по датам и сохраняем
			var messagesByDate = messagesList.GroupBy(m => m.Date.Date);
			
			foreach (var group in messagesByDate)
			{
				if (cancellationToken.IsCancellationRequested)
					break;
					
				await _markdownService.SaveMessagesAsync(group, chatTitle, group.Key, cancellationToken);
			}

			_logger.LogInformation("Архивирование чата {ChatTitle} завершено. Обработано {MessageCount} сообщений", 
				chatTitle, messagesList.Count);
				
			await _notificationService.SendInfoNotificationAsync(
				$"Архивирование чата '{chatTitle}' завершено. Обработано {messagesList.Count} сообщений.", cancellationToken);
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Архивирование чата {ChatId} отменено", chatId);
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при архивировании чата {ChatId}", chatId);
			await _notificationService.SendErrorNotificationAsync($"Ошибка архивирования чата {chatId}", ex, cancellationToken);
			throw;
		}
	}

	/// <summary>
	/// Инициализация клиента Telegram
	/// </summary>
	private async Task InitializeClientAsync()
	{
		if (_client != null)
			return;

		try
		{
			_logger.LogInformation("Инициализация клиента Telegram...");

			// Конфигурация клиента
			string Config(string what)
			{
				return what switch
				{
					"api_id" => _config.ApiId.ToString(),
					"api_hash" => _config.ApiHash,
					"phone_number" => _config.PhoneNumber,
					"session_pathname" => _config.SessionFile,
					_ => null!
				};
			}

			_client = new Client(Config);
			
			_logger.LogInformation("Клиент Telegram инициализирован");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при инициализации клиента Telegram");
			await _notificationService.SendErrorNotificationAsync("Ошибка инициализации клиента Telegram", ex);
			throw;
		}
	}

	/// <summary>
	/// Получить InputPeer для указанного ID чата
	/// </summary>
	private async Task<InputPeer> GetInputPeerFromChatId(long chatId)
	{
		// Если кэш чатов пуст, загружаем диалоги
		if (_allChats == null || _allUsers == null)
		{
			await GetChatsAsync();
		}

		// Ищем чат среди пользователей
		if (_allUsers != null)
		{
			var user = _allUsers.Values.FirstOrDefault(u => u.id == chatId);
			if (user != null)
			{
				return new InputPeerUser(user.id, user.access_hash);
			}
		}

		// Ищем среди чатов
		if (_allChats != null && _allChats.TryGetValue(chatId, out ChatBase? chat))
		{
			return chat switch
			{
				Chat c => new InputPeerChat(c.id),
				Channel ch => new InputPeerChannel(ch.id, ch.access_hash),
				_ => throw new ArgumentException($"Неизвестный тип чата: {chat.GetType()}")
			};
		}

		throw new ArgumentException($"Чат с ID {chatId} не найден");
	}

	/// <summary>
	/// Конвертация Telegram сообщения в ChatMessage
	/// </summary>
	private ChatMessage? ConvertTelegramMessageToChatMessage(MessageBase messageBase)
	{
		if (messageBase is not Message message)
			return null;

		var chatMessage = new ChatMessage
		{
			Id = message.id,
			Date = message.Date,
			AuthorId = message.from_id?.ID ?? 0,
			Text = message.message,
			Type = MessageType.Text
		};

		// Получаем имя автора
		if (message.from_id is PeerUser peerUser && _allUsers?.TryGetValue(peerUser.user_id, out User? user) == true)
		{
			chatMessage.AuthorName = GetUserDisplayName(user);
		}

		// Определяем тип сообщения и медиа
		if (message.media != null)
		{
			var mediaInfo = ConvertTelegramMediaToMediaInfo(message.media);
			if (mediaInfo != null)
			{
				chatMessage.Media = mediaInfo;
				chatMessage.Type = GetMessageTypeFromMedia(message.media);
			}
		}

		// Обработка ответов на сообщения
		if (message.reply_to is MessageReplyHeader replyHeader && replyHeader.reply_to_msg_id > 0)
		{
			chatMessage.ReplyToMessageId = replyHeader.reply_to_msg_id;
		}

		// Обработка пересланных сообщений
		if (message.fwd_from != null)
		{
			chatMessage.ForwardInfo = ConvertTelegramForwardInfo(message.fwd_from);
		}

		// Проверяем, было ли сообщение отредактировано
		if (message.edit_date != default)
		{
			chatMessage.IsEdited = true;
			chatMessage.EditDate = message.edit_date;
		}

		return chatMessage;
	}

	/// <summary>
	/// Конвертация медиа из Telegram в MediaInfo
	/// </summary>
	private MediaInfo? ConvertTelegramMediaToMediaInfo(MessageMedia media)
	{
		return media switch
		{
			MessageMediaPhoto photo when photo.photo is Photo p => new MediaInfo
			{
				FileSize = p.sizes?.OfType<PhotoSize>().FirstOrDefault()?.size,
				MimeType = "image/jpeg",
				Width = p.sizes?.OfType<PhotoSize>().FirstOrDefault()?.w,
				Height = p.sizes?.OfType<PhotoSize>().FirstOrDefault()?.h,
				FileName = $"photo_{p.id}.jpg"
			},
			MessageMediaDocument doc when doc.document is Document d => new MediaInfo
			{
				FileName = d.Filename ?? $"document_{d.id}",
				FileSize = d.size,
				MimeType = d.mime_type,
				Duration = GetDocumentDuration(d)
			},
			MessageMediaGeo => new MediaInfo
			{
				FileName = "location.geo",
				MimeType = "application/geo"
			},
			MessageMediaContact => new MediaInfo
			{
				FileName = "contact.vcf",
				MimeType = "text/vcard"
			},
			_ => null
		};
	}

	/// <summary>
	/// Определение типа сообщения по медиа
	/// </summary>
	private MessageType GetMessageTypeFromMedia(MessageMedia media)
	{
		return media switch
		{
			MessageMediaPhoto => MessageType.Photo,
			MessageMediaDocument doc when doc.document is Document d => d.mime_type switch
			{
				"video/mp4" or "video/mov" or "video/avi" => MessageType.Video,
				"audio/ogg" or "audio/mpeg" or "audio/wav" => MessageType.Voice,
				_ when IsDocumentSticker(d) => MessageType.Sticker,
				_ => MessageType.Document
			},
			MessageMediaPoll => MessageType.Poll,
			_ => MessageType.Unknown
		};
	}

	/// <summary>
	/// Конвертация информации о пересылке
	/// </summary>
	private ForwardInfo ConvertTelegramForwardInfo(MessageFwdHeader fwdHeader)
	{
		var forwardInfo = new ForwardInfo
		{
			OriginalDate = fwdHeader.date
		};

		if (fwdHeader.from_id is PeerUser peerUser && _allUsers?.TryGetValue(peerUser.user_id, out User? user) == true)
		{
			forwardInfo.FromName = GetUserDisplayName(user);
			forwardInfo.FromId = user.id;
		}
		else if (fwdHeader.from_name != null)
		{
			forwardInfo.FromName = fwdHeader.from_name;
		}

		return forwardInfo;
	}

	/// <summary>
	/// Получить отображаемое имя пользователя
	/// </summary>
	private static string GetUserDisplayName(User user)
	{
		if (!string.IsNullOrEmpty(user.first_name) && !string.IsNullOrEmpty(user.last_name))
		{
			return $"{user.first_name} {user.last_name}";
		}
		
		if (!string.IsNullOrEmpty(user.first_name))
		{
			return user.first_name;
		}
		
		if (!string.IsNullOrEmpty(user.last_name))
		{
			return user.last_name;
		}
		
		if (!string.IsNullOrEmpty(user.username))
		{
			return $"@{user.username}";
		}
		
		return $"Пользователь {user.id}";
	}

	/// <summary>
	/// Получить длительность документа
	/// </summary>
	private static int? GetDocumentDuration(Document document)
	{
		foreach (var attribute in document.attributes)
		{
			if (attribute is DocumentAttributeVideo video)
			{
				return (int)video.duration;
			}
			if (attribute is DocumentAttributeAudio audio)
			{
				return (int)audio.duration;
			}
		}
		return null;
	}

	/// <summary>
	/// Проверить, является ли документ стикером
	/// </summary>
	private static bool IsDocumentSticker(Document document)
	{
		return document.attributes.Any(attr => attr is DocumentAttributeSticker);
	}

	/// <summary>
	/// Асинхронное освобождение ресурсов
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;

		try
		{
			if (_client != null)
			{
				_client.Dispose();
				_logger.LogInformation("Ресурсы клиента Telegram освобождены");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Ошибка при освобождении ресурсов");
		}
		finally
		{
			_disposed = true;
		}
		
		await Task.CompletedTask;
	}
}