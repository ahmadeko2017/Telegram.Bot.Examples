using SeleniumTest;
using System.IO;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using File = System.IO.File;
using System.IO.Pipes;

namespace Telegram.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(ITelegramBotClient botClient, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        // Check if the update has a message
        if (update.Message != null)
        {
            _logger.LogWarning($"Ada user akses ni {update.Message.Chat.FirstName} {update.Message.Chat.LastName} {update.Message.Chat.Id}");
            // Extract the chat ID from the message
            long chatId = update.Message.Chat.Id;

            // Perform your chat ID verification here
            if (!IsAuthorizedChatId(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Id tidak terdaftar . . .",
                    cancellationToken: cancellationToken);
                // If the chat ID is not authorized, you can choose to ignore the update or handle it accordingly
                return;
            }

        }
        else if (update.CallbackQuery != null)
        {
            // Extract the chat ID from the callback query
            long chatId = update.CallbackQuery.Message.Chat.Id;

            // Perform your chat ID verification here
            if (!IsAuthorizedChatId(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Id tidak terdaftar . . .",
                    cancellationToken: cancellationToken);
                // If the chat ID is not authorized, you can choose to ignore the update or handle it accordingly
                return;
            }
        }

        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    // Define a list of authorized chat IDs (you can populate this list with your authorized chat IDs)
    private readonly List<long> authorizedChatIds = new List<long> { 657952763, 5162612990 };

    // Method to check if the chat ID is authorized
    private bool IsAuthorizedChatId(long chatId)
    {
        // Check if the provided chat ID is in the list of authorized chat IDs
        return authorizedChatIds.Contains(chatId);
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        var action = messageText.Split(' ')[0] switch
        {
            "/inline_test"     => SendInlineKeyboard(_botClient, message, cancellationToken),
            "/keyboard"        => SendReplyKeyboard(_botClient, message, cancellationToken),
            "/remove"          => RemoveKeyboard(_botClient, message, cancellationToken),
            "/photo"           => SendFile(_botClient, message, cancellationToken),
            "/request"         => RequestContactAndLocation(_botClient, message, cancellationToken),
            "/inline_mode"     => StartInlineQuery(_botClient, message, cancellationToken),
            "/test"            => TestWebsite(_botClient, message, cancellationToken),
            "/throw"           => FailingHandler(_botClient, message, cancellationToken),
            "/start"           => Start(_botClient, message, cancellationToken),
            _                  => HandleUserResponse(_botClient, message, cancellationToken)
            
        };
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendChatActionAsync(
                chatId: message.Chat.Id,
                chatAction: ChatAction.Typing,
                cancellationToken: cancellationToken);

            // Simulate longer running task
            await Task.Delay(500, cancellationToken);

            InlineKeyboardMarkup inlineKeyboard = new(
                new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Beranda", "Anda memilih beranda"),
                        InlineKeyboardButton.WithCallbackData("Search", "Cari sesuatu?"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Navigasi", "Mau kemana?"),
                        InlineKeyboardButton.WithCallbackData("Tutup", "Keluar"),
                    },
                });

            

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Pilih salah satu menu dibawah ini:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                })
            {
                ResizeKeyboard = true
            };

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Choose",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> HandleUserResponse(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            string responseText;
            string? imagePath = null;
            switch (message.Text)
            {
                case "Beranda":
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Sedang mengecheck mohon bersabar . . .",
                        cancellationToken: cancellationToken);

                    imagePath = HomeTesting.RunTest().ImagePath;
                    responseText = "Pengecekan beranda sukses . . .";
                    break;
                case "Search":
                    responseText = "Performing search...";
                    break;
                case "Navigation":
                    responseText = "Navigating...";
                    break;
                case "Oke":
                    responseText = "Okay!";
                    break;
                default:
                    responseText = "";
                    break;
            }

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using FileStream fileStream = new(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                return await botClient.SendDocumentAsync(
                    chatId: message.Chat.Id,
                    document: new InputFileStream(fileStream, imagePath),
                    caption: responseText,
                    cancellationToken: cancellationToken);
            }
            else
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: responseText,
                    cancellationToken: cancellationToken);
            }
        }

        static async Task<Message> TestWebsite(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                        new KeyboardButton[] { "Beranda", "Search" },
                        new KeyboardButton[] { "Navigation", "Oke" },
                })
            {
                ResizeKeyboard = true
            };

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Which one do you want to testing",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> RemoveKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Removing keyboard",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendChatActionAsync(
                message.Chat.Id,
                ChatAction.UploadPhoto,
                cancellationToken: cancellationToken);

            const string filePath = "Files/tux.png";
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

            return await botClient.SendPhotoAsync(
                chatId: message.Chat.Id,
                photo: new InputFileStream(fileStream, fileName),
                caption: "Nice Picture",
                cancellationToken: cancellationToken);
        }

        static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup RequestReplyKeyboard = new(
                new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Who or Where are you?",
                replyMarkup: RequestReplyKeyboard,
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            const string usage = "Usage:\n" +
                                 "/inline_keyboard - send inline keyboard\n" +
                                 "/keyboard    - send custom keyboard\n" +
                                 "/remove      - remove custom keyboard\n" +
                                 "/photo       - send a photo\n" +
                                 "/request     - request location or contact\n" +
                                 "/inline_mode - send keyboard with Inline Query";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> Start(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            string start = $"Halo {message.Chat.FirstName},\n" +
               "Selamat datang di chat bot. Disini saya akan membantu Anda untuk:\n" +
               "1. /test - Melakukan monitoring aplikasi.\n" +
               "2. /inline_test - Melakukan testing sederhana.\n\n" +
               "Jika Anda membutuhkan bantuan lebih lanjut, jangan ragu untuk bertanya.";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: start,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new(
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Press the button to start Inline Query",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        static Task<Message> FailingHandler(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            throw new IndexOutOfRangeException();
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        // Extract callback data and handle it
        string callbackData = callbackQuery.Data;

        string responseText = callbackData switch
        {
            "Anda memilih beranda" => "Anda telah memilih Beranda.",
            "Cari sesuatu?" => "Anda memilih untuk mencari sesuatu.",
            "Mau kemana?" => "Anda memilih Navigasi.",
            "Keluar" => "Anda memilih untuk keluar.",
            _ => "Pilihan tidak dikenali."
        };

        // Send a response message
        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message.Chat.Id,
            text: responseText,
            cancellationToken: cancellationToken
        );

        // Optionally, you can also acknowledge the callback query
        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Pilihan diterima",
            cancellationToken: cancellationToken
        );
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}
