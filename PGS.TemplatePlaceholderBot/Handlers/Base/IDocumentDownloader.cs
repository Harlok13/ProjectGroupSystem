using PGS.TemplatePlaceholderBot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;
using TelegramFile = Telegram.Bot.Types.File;

namespace PGS.TemplatePlaceholderBot.Handlers.Base;

public interface IDocumentDownloader
{
    public static async Task<string> DownloadDocumentAsync(ITelegramBotClient botClient, Document document, CancellationToken cT)
    {
        TelegramFile fileInfo = await botClient.GetFileAsync(document.FileId, cT);
        
        string filePath = $"{EnvironmentHelper.GetVolumePath()}/{document.FileName}";
        
        await using FileStream fStream = File.OpenWrite(filePath);
        
        await botClient.DownloadFileAsync(
            filePath: fileInfo.FilePath,
            destination: fStream,
            cancellationToken: cT
        );

        return filePath;
    }
    
    public static async Task<string> DownloadTemplateDocumentAsync(ITelegramBotClient botClient, Document document, CancellationToken cT)
    {
        TelegramFile fileInfo = await botClient.GetFileAsync(document.FileId, cT);
        
        string filePath = $"{EnvironmentHelper.GetTemplatesVolumePath()}/{document.FileName}";
        
        await using FileStream fStream = File.OpenWrite(filePath);
        
        await botClient.DownloadFileAsync(
            filePath: fileInfo.FilePath,
            destination: fStream,
            cancellationToken: cT
        );

        return filePath;
    }
}