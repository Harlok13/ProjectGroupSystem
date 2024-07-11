using System.IO.Compression;
using Aspose.Cells;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace PGS.TemplatePlaceholderBot;

public static class Hadlers
{
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        
        if (update.Message.Text != string.Empty)
        {
            Console.WriteLine("hi");
        }
    //
    //     if (message.Type != MessageType.Document)
    //         return;
    //

        // var fileId = message.Document.FileId;
        // var excelPath = "/app/excel/documents";
        // var excelPath = "/Users/Harlok/Desktop";
        // var tempPath = Path.GetTempPath();
        // Directory.CreateDirectory(tempPath);
        // var file = await botClient.GetFileAsync(fileId, cancellationToken);
        // var filePath = Path.Combine(tempPath, file.FilePath);
        // await using var st = new FileStream(filePath, FileMode.Create);
        // await botClient.DownloadFileAsync(file.FilePath, st, cancellationToken);


        var fileInfo = await botClient.GetFileAsync(message.Document.FileId);
    // Download file from server (step 2)
        // await using (var fileStream = System.IO.File.OpenWrite("/Users/Harlok/Desktop/file.xslx"))
        await using (var fileStream = System.IO.File.OpenWrite("/file.xlsx"))
        {
            await botClient.DownloadFileAsync(
                filePath: fileInfo.FilePath,
                destination: fileStream
            );
        }

        // parse the file using Aspose.Cells
        // var workbook = new Workbook(filePath);
        // var workbook = new Workbook(fileInfo.FilePath);
        var workbook = new Workbook("/file.xlsx");
        var sheet = workbook.Worksheets[0];

        var fileGuids = new List<Guid>();
        for (int rowIndex = 1; rowIndex <= sheet.Cells.MaxRow; rowIndex++)
        {
            // Получаем текущую строку
            Row row = sheet.Cells.Rows[rowIndex];

            // Обходим все ячейки в текущей строке
            var wReader = new WordReader();
            // var templatePath = "/Users/Harlok/Desktop/template.docx";
            var templatePath = $"{Directory.GetCurrentDirectory()}/template.docx";
            var foo = wReader.GetKeywords(templatePath);

            var keywoards = new Dictionary<string, string>();

            var i = 0;

            // wReader.GetKeywords("/Users/Harlok/Desktop/template.docx").ForEach(Console.WriteLine);
            foreach (Cell cell in row)
            {
                // Выводим значение ячейки в консоль
                keywoards.Add(foo[i], cell.Value.ToString());
                Console.Write(cell.Value + "\t");
                i++;
            }

            i = 0;
            // Переходим на новую строку после вывода всех ячеек текущей строки
            Console.WriteLine();
            var fileGuid = wReader.FillWord(templatePath, keywoards);
            fileGuids.Add(fileGuid);
        }

        var tempDir = Path.GetTempPath();


    // Добавляем правило для разрешения полного доступа для текущего пользователя
        // Syscall.chmod(tempDir, FilePermissions.ALLPERMS);

        Directory.CreateDirectory(tempDir);

        // ProcessStartInfo startInfo = new ProcessStartInfo() 
        // {
        //     FileName = "/bin/bash",
        //     Arguments = $"-c \" sudo chmod +x  {tempDir}\" ",
        //
        //     CreateNoWindow = true
        // };
        //
        // Process proc = new Process() { StartInfo = startInfo, };
        // proc.Start();

        string archiveFileName = $"{tempDir}/documents.zip";
        await using (FileStream archiveFile = File.Create(archiveFileName))
        {
            using (ZipArchive archive = new ZipArchive(archiveFile, ZipArchiveMode.Create))
            {
                foreach (var fileGuid in fileGuids)
                {
                    // string path = Path.Combine(Environment.UserDomainName, Environment.UserName, "Desktop", "words",
                    //     $"{fileGuid}.docx");
                    // string path = $"/Users/Harlok/Desktop/words/{fileGuid}.docx";
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

        // Удаляем временную директорию и все файлы в ней
        // Directory.Delete(tempDir, true);
    }

    public static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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
}