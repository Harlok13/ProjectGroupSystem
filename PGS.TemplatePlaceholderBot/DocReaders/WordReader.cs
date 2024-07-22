using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PGS.TemplatePlaceholderBot.Helpers;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using APIC = Aspose.Cells.Drawing;

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
    
    public List<string> CreateWordsAndFillByTemplate(List<Dictionary<string, object>> keyValuePairsList)
    {
        List<string> resultFilePaths = new();

        int fileNameIndex = 1;
        foreach (Dictionary<string, object> keyValuePairs in keyValuePairsList)
        {
            var fileName = keyValuePairs["FileName"];
            var resultFilePath = $"{EnvironmentHelper.GetVolumePath()}/{fileName}_{fileNameIndex}.docx";
            CreateResultDoc(resultFilePath);
            using WordprocessingDocument resultDoc = WordprocessingDocument.Open(path: resultFilePath, isEditable: true);

            if (resultDoc.MainDocumentPart?.Document.Body is not { } resultBody)
                throw new ArgumentNullException("The body of the word document is missing.");
            
            #region process
            foreach (KeyValuePair<string, object> pair in keyValuePairs)
            {
                foreach (Paragraph paragraph in resultBody.Elements<Paragraph>())
                {
                    string text = string.Join(string.Empty, paragraph.Descendants<Text>()
                        .Select(t => t.Text));
            
                    if (!text.Contains($"<{pair.Key}>"))
                        continue;
            
                    if (pair.Value is APIC.Picture pic)
                    {
                        InsertPicture(pic, resultDoc.MainDocumentPart, paragraph);
                        text = text.Replace($"<{pair.Key}>", string.Empty);
                    }
                    else
                    {
                        text = text.Replace($"<{pair.Key}>", pair.Value.ToString());
                    }
                    
                    foreach (Text t in paragraph.Descendants<Text>().ToList())
                    {
                        t.Remove();
                    }
            
                    paragraph.Append(new Run(new Text(text)));
                }
            }
            #endregion
            
            resultDoc.Save();

            resultFilePaths.Add(resultFilePath);

            fileNameIndex++;
        }
        
        return resultFilePaths;
    }
    
    private void CreateResultDoc(string resultFilePath)
    {
        using WordprocessingDocument resultDoc = WordprocessingDocument
            .Create(resultFilePath, WordprocessingDocumentType.Document);
        
        OpenXmlElement templateBody = _doc.MainDocumentPart?.Document.Body?.CloneNode(deep: true) 
                                      ?? throw new Exception("The body of the word document is missing.");

        resultDoc.AddMainDocumentPart();
        resultDoc.MainDocumentPart!.Document = new Document(templateBody);
        resultDoc.MainDocumentPart.Document.Save();
    }

    #region Picture
    
    private void InsertPicture(APIC.Picture pic, MainDocumentPart docPart, Paragraph paragraph)
    {
        using var ms = new MemoryStream(pic.Data);
        var imagePart = docPart.AddImagePart(ImagePartType.Jpeg);
        imagePart.FeedData(ms);
        
        var relationshipId = docPart.GetIdOfPart(imagePart);
            
        var drawing = CreateDrawingElement(pic, relationshipId);
            
        var currentRun = paragraph.Elements<Run>().LastOrDefault();
        currentRun?.Append(drawing);
    }
    
    static Drawing CreateDrawingElement(APIC.Picture picture, string relationshipId)
    {
        long width = picture.Width * 20000;
        long height = picture.Height * 20000;
        
        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent() { Cx = width, Cy = height },
                new DW.EffectExtent()
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L
                },
                new DW.DocProperties()
                {
                    Id = (UInt32Value)1U,
                    Name = "Picture 1"
                },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks() { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties()
                                    {
                                        Id = (UInt32Value)0U,
                                        Name = "New Bitmap Image.jpg"
                                    },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip(
                                        new A.BlipExtensionList(
                                            new A.BlipExtension()
                                            {
                                                Uri =
                                                    "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                            })
                                    )
                                    {
                                        Embed = relationshipId,
                                        CompressionState =
                                            A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(
                                        new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = width, Cy = height }),
                                    new A.PresetGeometry(
                                            new A.AdjustValueList()
                                        )
                                        { Preset = A.ShapeTypeValues.Rectangle }))
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            )
            {
                DistanceFromTop = (UInt32Value)0U,
                DistanceFromBottom = (UInt32Value)0U,
                DistanceFromLeft = (UInt32Value)0U,
                DistanceFromRight = (UInt32Value)0U,
                EditId = "50D07946"
            });

        return drawing;
    }
    
    #endregion
    
    #region DisposePattern

    private bool _disposed;

    ~WordReader()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _doc.Dispose();
        }
        
        _disposed = true;
    }
    
    #endregion
}