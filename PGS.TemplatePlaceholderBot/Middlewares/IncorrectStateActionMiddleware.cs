using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Enums;
using PGS.TemplatePlaceholderBot.Keyboards;
using PGS.TemplatePlaceholderBot.Middlewares.Base;
using PGS.TemplatePlaceholderBot.States;
using PGS.TemplatePlaceholderBot.Storage;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Middlewares;

public class IncorrectStateActionMiddleware(IMemoryCache _cache, IFileStorage _storage) : IMiddleware
{
    public async Task HandleUpdateAsync(Update update, User user, Func<Task> next, CancellationToken cT)
    {
        switch (user.StateMachine.State)
        {
            case EBotState.WaitingTemplateNumberForChoice:
                if (update.Message is not { } message) return;
                if (update.Message.Text is not { } text) return;

                if (!int.TryParse(text, out int templateIndex))
                {
                    user.FillIncorrectStateActionData(
                        message: "Я ожидаю номер шаблона, Вы должны отправить число.",
                        inlineKeyboard: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel));
                    
                    _cache.UpdateUser(message.Chat.Id, user);
                    
                    goto default;
                }
                
                if (templateIndex >= _storage.GetTemplatesCount() || templateIndex < 0)
                {
                    user.FillIncorrectStateActionData(
                        message: "Шаблона с таким номером нет, введите другое число.",
                        inlineKeyboard: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel));
                    
                    _cache.UpdateUser(message.Chat.Id, user);
                }

                break;

            case EBotState.Default:
                break;
            case EBotState.WaitingTemplateNumberForDelete:
                break;
            case EBotState.WaitingTemplateForCreatingExcel:
                break;
            default:
                break;
        }

        await next();
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken cT)
    {
        throw new NotImplementedException();
    }
}