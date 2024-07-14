using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Constants;
using PGS.TemplatePlaceholderBot.DocReaders;
using PGS.TemplatePlaceholderBot.Handlers.Base;
using PGS.TemplatePlaceholderBot.Helpers;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PGS.TemplatePlaceholderBot.Handlers;

public class WordTemplateHandler(IMemoryCache _cache) : IUpdateHandler, IDocumentDownloader
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is not { } message)
            return;

        if (message.Document is { } document)
        {
            if (document.MimeType != MimeConstants.Docx)
                return;
            
            Log.Information("Received word file - {DocName}", document.FileName);

            // TODO: add state machine. check is file exist
            string filePath = await IDocumentDownloader
                .DownloadTemplateDocumentAsync(botClient, document, cT);

            using WordReader wordReader = new WordReader(filePath);
            List<string> keywords = wordReader.GetKeywords();
            
            if (!keywords.Any())
            {
                Log.Information("\"{DocName}\" file is not a template.", document.FileName);
                
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Файл \"{message.Document.FileName}\" не является шаблоном. Шаблон должен содержать фигурные скобки <> для слов, которые необходимо заменить",
                    cancellationToken: cT);
                
                FileDeleteHelper.DeleteFile(filePath);

                return;
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Шаблон \"{message.Document.FileName}\" успешно загружен",
                cancellationToken: cT);
            
            Log.Information("Template \"{DocName}\" successfully loaded.", document.FileName);

            _cache.SetCurrentTemplate(message.Document.FileName ?? "");
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Текущий шаблон изменен на \"{message.Document.FileName}\"",
                cancellationToken: cT);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}