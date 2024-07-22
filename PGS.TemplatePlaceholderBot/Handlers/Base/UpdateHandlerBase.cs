using Serilog;
using Telegram.Bot;

namespace PGS.TemplatePlaceholderBot.Handlers.Base;

public abstract class UpdateHandlerBase
{
    public virtual Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        string message = exception switch
        {
            _ => exception.ToString()
        };
        
        Log.Error(message);
        if (exception.StackTrace is not null)
            Log.Error(exception.StackTrace);
        
        return Task.CompletedTask;
    }
}