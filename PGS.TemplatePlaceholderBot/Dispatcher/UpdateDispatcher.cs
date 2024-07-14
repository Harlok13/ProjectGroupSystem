using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Middlewares.Base;
using PGS.TemplatePlaceholderBot.States;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Dispatcher;

public class UpdateDispatcher(
    IMemoryCache _cache,
    IEnumerable<IUpdateHandler> _updateHandlers,
    IEnumerable<IMiddleware> _middlewares)
{
    public async Task DispatchAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        // await HandleMiddlewareAsync(botClient, update, cT);
        await HandleCallbackAsync();
        await HandleUpdateAsync(botClient, update, cT);
    }
    
    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cT)
    {
        string errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.Error(errorMessage);
        
        return Task.CompletedTask;
    }

    private async Task HandleMiddlewareAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message?.Chat.Id is not null)
        {
            Func<Task> next = async () => await Task.CompletedTask;
            User user = _cache.GetUser(update.Message.Chat.Id);
            
            foreach (IMiddleware middleware in _middlewares.Reverse())
            {
                try
                {
                    next = async () =>
                    {
                        await middleware.HandleUpdateAsync(update, user, next, cT);
                    };
                }
                catch (Exception ex)
                {
                    await middleware.HandlePollingErrorAsync(botClient, ex, cT);
                }
            }
            
            await next();
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        foreach (IUpdateHandler updateHandler in _updateHandlers)
        {
            try
            {
                await updateHandler.HandleUpdateAsync(botClient, update, cT);
            }
            catch (Exception ex)
            {
                await updateHandler.HandlePollingErrorAsync(botClient, ex, cT);
            }
        }
    }

    private async Task HandleCallbackAsync()
    {
        
    }
}