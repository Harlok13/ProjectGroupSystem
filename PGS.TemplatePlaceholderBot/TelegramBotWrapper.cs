using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Dispatcher;
using PGS.TemplatePlaceholderBot.Handlers;
using PGS.TemplatePlaceholderBot.Handlers.Callbacks;
using PGS.TemplatePlaceholderBot.Helpers;
using PGS.TemplatePlaceholderBot.Middlewares;
using PGS.TemplatePlaceholderBot.Middlewares.Base;
using PGS.TemplatePlaceholderBot.Storage;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace PGS.TemplatePlaceholderBot;

public class TelegramBotWrapper
{
    public TelegramBotClient CreateBotClient()
    {
        CancellationTokenSource cts = new();
        CancellationToken cT = cts.Token;

        IFileStorage storage = new LocalFileStorage();
        IMemoryCache cache = new MemoryCache(storage);
        
        TelegramBotClient botClient = new(EnvironmentHelper.GetBotToken());
        UpdateDispatcher dispatcher = new(
            cache,
            RegisterUpdateHandlers(cache, storage),
            RegisterMiddlewares());
        
        botClient.StartReceiving(
            updateHandler: dispatcher.DispatchAsync,
            pollingErrorHandler: dispatcher.HandlePollingErrorAsync,
            cancellationToken: cT,
            receiverOptions: new ReceiverOptions
            {
                AllowedUpdates = [] 
            });

        return botClient;
    }

    private IUpdateHandler[] RegisterUpdateHandlers(IMemoryCache cache, IFileStorage storage)
    {
        IUpdateHandler[] handlers =
        [
            new DocumentHandler(cache),  // must come first before document handlers
            new WordTemplateHandler(cache),
            new ExcelDataHandler(cache),
            new TextMessageHandler(cache, storage),
            new TemplateMenuCallbackHandler(cache, storage),
        ];

        return handlers;
    }

    private IMiddleware[] RegisterMiddlewares()
    {
        IMiddleware[] middlewares =
        [
            new TemplateNumberForDeleteMiddleware(),
            new TemplateNumberForChoiceMiddleware(),
        ];

        return middlewares;
    }
}