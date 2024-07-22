using PGS.TemplatePlaceholderBot.Cache;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Handlers;

/// <summary>
///     Processes messages that dont meet a status condition.
/// </summary>
public class IncorrectStateActionHandler(IMemoryCache _cache) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is not { } message) return;

        User user = _cache.GetUser(message.Chat.Id);
        if (!user.IsIncorrectStateAction)
            return;

        (string? answer, InlineKeyboardMarkup? inlineKeyboard) = user.GetIncorrectStateActionData();
        
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: answer,
            replyMarkup: inlineKeyboard,
            cancellationToken: cT);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cT)
    {
        throw new NotImplementedException();
    }
}