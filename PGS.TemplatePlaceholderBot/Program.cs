using System.IO.Compression;
using Aspose.Cells;
using PGS.TemplatePlaceholderBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using File = System.IO.File;

var botClient = new TelegramBotClient("6188341346:AAGE65eiFiTEpM7DC9xzDU6e68EoQu2xOdg");

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: new ReceiverOptions
    {
        AllowedUpdates = [] 
    });

Console.WriteLine($"StartBot - {botClient.BotId}");

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    try
    {
        if (update.Message is not { } message)
            return;

        if (message.Document != null)
        {
            var fileInfo = await botClient.GetFileAsync(message.Document.FileId);
            await using (var fileStream = File.OpenWrite("/app/files/file.xlsx"))
            {
                await botClient.DownloadFileAsync(
                    filePath: fileInfo.FilePath,
                    destination: fileStream
                );
            }

            var workbook = new Workbook("/app/files/file.xlsx");
            var sheet = workbook.Worksheets[0];

            var fileGuids = new List<Guid>();
            for (int rowIndex = 1; rowIndex <= sheet.Cells.MaxRow; rowIndex++)
            {
                Row row = sheet.Cells.Rows[rowIndex];

                var wReader = new WordReader();
                var templatePath = $"/app/files/template.docx";
                var foo = wReader.GetKeywords(templatePath);

                var keywoards = new Dictionary<string, string>();

                var i = 0;

                foreach (Cell cell in row)
                {
                    keywoards.Add(foo[i], cell.Value.ToString());
                    Console.Write(cell.Value + "\t");
                    i++;
                }
                i = 0;
                var fileGuid = wReader.FillWord(templatePath, keywoards);
                fileGuids.Add(fileGuid);
            }

            var tempDir = Path.GetTempPath();

            
            Directory.CreateDirectory(tempDir);
            
            string archiveFileName = $"{tempDir}/documents.zip";
            await using (FileStream archiveFile = File.Create(archiveFileName))
            {
                using (ZipArchive archive = new ZipArchive(archiveFile, ZipArchiveMode.Create))
                {
                    foreach (var fileGuid in fileGuids)
                    {
                        string path = $"{Directory.GetCurrentDirectory()}/{fileGuid}.docx";
                        archive.CreateEntryFromFile(path, fileGuid + ".docx");
                    }
                }
            }

            await using (FileStream archiveStream = File.OpenRead(archiveFileName))
            {
                await botClient.SendDocumentAsync(
                    chatId: update.Message.Chat.Id,
                    document: new InputFileStream(archiveStream, "documents.zip"),
                    caption: "Здесь новые ворд-документы"
                );
            }
            
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    // Удаляем временную директорию и все файлы в ней
    // Directory.Delete(tempDir, true);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.Error.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

Console.Read();