using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Constants;
using PGS.TemplatePlaceholderBot.DocReaders;
using PGS.TemplatePlaceholderBot.Handlers.Base;
using PGS.TemplatePlaceholderBot.Helpers;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace PGS.TemplatePlaceholderBot.Handlers;

public class ExcelDataHandler(IMemoryCache _cache) : UpdateHandlerBase, IUpdateHandler, IDocumentDownloader
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is not { } message)
            return;

        if (message.Document is { } document)
        {
            if (document.MimeType != MimeConstants.Xlsx)
                return;
            
            Log.Information("Received a message containing a excel document \"{DocName}\"", document.FileName);
            
            try
            {
                string? templateName = _cache.GetCurrentTemplateName();
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Вы не выбрали шаблон. Пожалуйста, загрузите или выберите шаблон.",
                        cancellationToken: cT);

                    Log.Information("You have not selected a template. Please upload or select a template.");
                    
                    return;
                }
                
                string excelPath = await IDocumentDownloader
                    .DownloadDocumentAsync(botClient, document, cT);
                
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Файл обрабатывается, дождитесь архива с готовыми документами.",
                    cancellationToken: cT);

                string archiveName = GetFormattedArchiveName(templateName);
                string templatePath = $"{EnvironmentHelper.GetTemplatesVolumePath()}/{_cache.GetCurrentTemplateName()}";
                string archivePath = $"{EnvironmentHelper.GetVolumePath()}/{archiveName}";

                using (ExcelReader excelReader = ExcelReader.Open(excelPath))
                {
                    // List<string> resultDocPaths = excelReader.CreateAndFillDocs(templatePath);
                    List<Dictionary<string, object>> keyValuePairsList = excelReader.GetKeyValuePairs(templatePath);
                    using WordReader wr = new WordReader(templatePath);
                    List<string> resultDocPaths = wr.CreateWordsAndFillByTemplate(keyValuePairsList);

                    using (ZipArchiveHelper zipArchiveHelper = new ZipArchiveHelper(archivePath))
                    {
                        Log.Information("Archive creation has begun. Archive name: {ArchiveName}", archivePath);
                        zipArchiveHelper.FillZipArchive(resultDocPaths);
                    }

                    await using FileStream archiveStream = File.OpenRead(archivePath);
                    await botClient.SendDocumentAsync(
                        chatId: update.Message.Chat.Id,
                        document: new InputFileStream(archiveStream, archiveName),
                        caption: $"Здесь новые ворд-документы, заполненные данными из \"{GetExcelFileName(excelPath)}\" на основе шаблона \"{templateName}\"\nКоличество документов в архиве: {resultDocPaths.Count}",
                        cancellationToken: cT
                    );

                    Log.Information("Sent archive \"{ArchiveName}\" with ready-made word documents.", archiveName);
                    
                    FileDeleteHelper.DeleteFiles(resultDocPaths);
                }

                await botClient.DeleteMessageAsync(
                    chatId: message.Chat.Id,
                    messageId: sentMessage.MessageId,
                    cancellationToken: cT);

                FileDeleteHelper.DeleteFile(archivePath);
                FileDeleteHelper.DeleteFile(excelPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                if (ex.StackTrace is not null)
                    Log.Error(ex.StackTrace);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Что-то пошло не так, попробуйте повторить попытку.",
                    cancellationToken: cT);
            }
        }
    }

    private string GetExcelFileName(string excelPath) =>
        excelPath.Split(Path.DirectorySeparatorChar)[^1];

    private string GetFormattedArchiveName(string templateName) =>
        $"{templateName.Split('.')[0]}_archive_{DateTime.Now:dd-mm-yyyy}.zip";
}