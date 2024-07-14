using PGS.TemplatePlaceholderBot.Middlewares.Base;
using PGS.TemplatePlaceholderBot.States;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Middlewares;

public class TemplateNumberForChoiceMiddleware : IMiddleware
{
    public async Task HandleUpdateAsync(Update update, User user, Func<Task> next, CancellationToken cT)
    {
        if (user.StateMachine.State != EBotState.WaitingTemplateNumberForChoice)
            return;
        
        Log.Information("EBotState.WaitingTemplateNumberForChoice");
        
        await user.StateMachine.FireAsync(EBotTrigger.Cancel, cT);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cT)
    {
        throw new NotImplementedException();
    }
}