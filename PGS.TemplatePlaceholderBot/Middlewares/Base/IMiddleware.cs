using Telegram.Bot;
using Telegram.Bot.Types;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Middlewares.Base;

public interface IMiddleware
{
    Task HandleUpdateAsync(Update update, User user, Func<Task> next, CancellationToken cT);

    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cT);
}