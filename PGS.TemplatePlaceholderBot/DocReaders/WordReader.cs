using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PGS.TemplatePlaceholderBot.Helpers;

namespace PGS.TemplatePlaceholderBot.DocReaders;

public class WordReader(string _filePath) : IDisposable
{
    private readonly WordprocessingDocument _doc = WordprocessingDocument.Open(_filePath, false);

    public List<string> GetKeywords()
    {
        List<string> keywords = new();
        Body body = _doc.MainDocumentPart?.Document.Body
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

    public string CreateWordAndFillByTemplate(Dictionary<string, string> keywords)
    {
        var resultFileGuid = Guid.NewGuid();
        var resultFilePath = $"{EnvironmentHelper.GetVolumePath()}/{resultFileGuid.ToString()}.docx";
        
        OpenXmlElement templateBody = _doc.MainDocumentPart?.Document.Body?.CloneNode(deep: true) 
            ?? throw new Exception("The body of the word document is missing.");
        
        foreach (var value in keywords)
        {
            foreach (Paragraph paragraph in templateBody.Elements<Paragraph>())
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

        CreateResultDoc(resultFilePath, templateBody);

        return resultFilePath;
    }

    private void CreateResultDoc(string resultFilePath, OpenXmlElement templateBody)
    {
        using WordprocessingDocument resultDoc = WordprocessingDocument
            .Create(resultFilePath, WordprocessingDocumentType.Document);
        
        resultDoc.AddMainDocumentPart();
        resultDoc.MainDocumentPart!.Document = new Document(templateBody);
        resultDoc.MainDocumentPart.Document.Save();
    }

    public void Dispose()
    {
        _doc.Dispose();
        GC.SuppressFinalize(this);
    }
}