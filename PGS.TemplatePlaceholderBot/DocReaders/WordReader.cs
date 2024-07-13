using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PGS.TemplatePlaceholderBot.DocReaders;

public static class WordReader
{
    public static List<string> GetKeywords(string filePath)
    {
        using WordprocessingDocument doc = WordprocessingDocument
            .Open(filePath, false);

        List<string> keywords = new();
        Body body = doc.MainDocumentPart?.Document.Body
            ?? throw new Exception("The body of the word document is missing.");
        
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
        }

        return keywords;
    }

    public static Guid FillWordTemplate(string templatePath, Dictionary<string, string> keywords)
    {
        var resultFileGuid = Guid.NewGuid();
        var resultFilePath = $"{Directory.GetCurrentDirectory()}/{resultFileGuid.ToString()}.docx";
        
        using WordprocessingDocument templateDoc = WordprocessingDocument
            .Open(templatePath, false);
        
        MainDocumentPart templateDocPart = templateDoc.MainDocumentPart;
        
        Body templateBody = templateDocPart.Document.Body;
        foreach (var value in keywords)
        {
            foreach (Paragraph paragraph in templateDocPart.Document.Body.Elements<Paragraph>())
            {
                string text = string.Join(string.Empty, paragraph.Descendants<Text>()
                    .Select(t => t.Text));

                if (!text.Contains($"<{value.Key}>")) 
                    continue;
                
                text = text.Replace($"<{value.Key}>", value.Value);

                foreach (Text t in paragraph.Descendants<Text>().ToList())
                {
                    t.Remove();
                }

                paragraph.Append(new Run(new Text(text)));
            }
        }

        using WordprocessingDocument resultDoc = WordprocessingDocument
            .Create(resultFilePath, WordprocessingDocumentType.Document);
        
        resultDoc.AddMainDocumentPart();
        resultDoc.MainDocumentPart.Document = new Document(templateBody.CloneNode(true));
        resultDoc.MainDocumentPart.Document.Save();

        return resultFileGuid;
    }
}