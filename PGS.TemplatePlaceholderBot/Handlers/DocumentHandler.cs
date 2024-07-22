using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Constants;
using PGS.TemplatePlaceholderBot.Handlers.Base;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PGS.TemplatePlaceholderBot.Handlers;

public class DocumentHandler(IMemoryCache _cache) : UpdateHandlerBase,IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is not { } message)
            return;

        if (update.Message.Document is { } document 
            && document.MimeType != MimeConstants.Docx 
            && document.MimeType != MimeConstants.Xlsx)
        {
            Log.Information("Received a message containing a document with mime type: {MimeType}", document.MimeType);
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Я принимаю только документы с расширением docx для ворд шаблонов и xlsx, содержащий данные для шаблона",
                cancellationToken: cT);
        }
    }
}