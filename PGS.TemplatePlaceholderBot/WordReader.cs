using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TelegramBot;

public class WordReader : IWordReader
{
    // private readonly WordprocessingDocument _doc;

    public WordReader()
    {
        // using WordprocessingDocument doc = WordprocessingDocument.Open(
        //     "/Users/Harlok/Desktop/template.docx", false);
        //
        // _doc = doc;
    }

    public List<string> GetKeywords(string filePath)
    {
        using WordprocessingDocument doc = WordprocessingDocument
            .Open(filePath, false);

        List<string> keywords = new();
        Body body = doc.MainDocumentPart.Document.Body;
        foreach (Paragraph paragraph in body.Elements<Paragraph>())
        {
            Regex regex = new("<(.*?)>");
            MatchCollection matches = regex.Matches(paragraph.InnerText);

            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                if (!keywords.Contains(key))
                    keywords.Add(key);
            }
            Console.WriteLine(paragraph.InnerText);
        }

        return keywords;
    }

    public Guid FillWord(string templatePath, Dictionary<string, string> keywords)
    {
        var fileGuid = Guid.NewGuid();
        // var filePath = $"/Users/Harlok/Desktop/words/{fileGuid.ToString()}.docx";
        var filePath = $"{Directory.GetCurrentDirectory()}/{fileGuid.ToString()}.docx";
        
        using WordprocessingDocument templateDoc = WordprocessingDocument
            .Open(templatePath, false);
        // Получаем основной документ
        MainDocumentPart mainPart = templateDoc.MainDocumentPart;

        // Получаем тело документа
        Body body = mainPart.Document.Body;
        foreach (var value in keywords)
        {
            foreach (Paragraph paragraph in mainPart.Document.Body.Elements<Paragraph>())
            {
                // Объединяем все текстовые фрагменты в один
                string text = string.Join(string.Empty, paragraph.Descendants<Text>().Select(t => t.Text));

                // Заменяем закладки в тексте
                if (text.Contains($"<{value.Key}>"))
                {
                    text = text.Replace($"<{value.Key}>", value.Value);

                    // Удаляем все текстовые фрагменты из абзаца
                    foreach (Text t in paragraph.Descendants<Text>().ToList())
                    {
                        t.Remove();
                    }

                    // Добавляем новый текстовый фрагмент с замененным текстом
                    paragraph.Append(new Run(new Text(text)));
                }
            }
        }

        // Сохраняем измененный документ в новый файл
        using WordprocessingDocument newDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        newDoc.AddMainDocumentPart();
        newDoc.MainDocumentPart.Document = new Document(body.CloneNode(true));
        newDoc.MainDocumentPart.Document.Save();

        return fileGuid;
    }
}