using System.Text.RegularExpressions;
using Aspose.Words;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PGS.TemplatePlaceholderBot.Helpers;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using APIC = Aspose.Cells.Drawing;
using Body = DocumentFormat.OpenXml.Wordprocessing.Body;
using Document = Aspose.Words.Document;
using Justification = DocumentFormat.OpenXml.Math.Justification;
using JustificationValues = DocumentFormat.OpenXml.Math.JustificationValues;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;

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
            using WordprocessingDocument
                resultDoc = WordprocessingDocument.Open(path: resultFilePath, isEditable: true);

            if (resultDoc.MainDocumentPart?.Document.Body is not { } resultBody)
                throw new ArgumentNullException("The body of the word document is missing.");

            // var runProperties = FindMostCommonStyle(_doc);

            #region process
            foreach (KeyValuePair<string, object> pair in keyValuePairs)
            {
                foreach (var footerPart in resultDoc.MainDocumentPart.FooterParts)
                {
                    var textElements = footerPart.Footer.Descendants<Text>();
                    foreach (var textElement in textElements)
                    {
                        if (!textElement.Text.Contains(pair.Key))
                            continue;

                        textElement.Text = textElement.Text.Replace($"<{pair.Key}>", pair.Value.ToString());
                    }
                    
                    footerPart.Footer.Save();
                }
                
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
                    
                    var parts = text.Split("\n");
                    if (parts.Length > 1)
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (i > 0)
                            {
                                paragraph.AppendChild(new Run(new Break()));
                                var paragraphProperties = new ParagraphProperties(
                                    new Indentation() { Left = "7200" } // 720 twips = 0.5 inches
                                );
                                paragraph.PrependChild(paragraphProperties);
                            }
                        
                            var run = new Run(new Text(parts[i]));
                            // paragraph.AppendChild(new Run(new Text(parts[i]))
                            // {
                            //     RunProperties = GetRunProperties()
                            // });
                            run.PrependChild(GetRunProperties());
                            
                            var paragraphProperties2 = new ParagraphProperties(
                                new DocumentFormat.OpenXml.Wordprocessing.Justification() { Val = DocumentFormat.OpenXml.Wordprocessing.JustificationValues.Left }
                            );
                            paragraph.PrependChild(paragraphProperties2);
                            paragraph.AppendChild(run);
                        }
                    }
                    else
                    {
                        var newRun = new Run(new Text(text));
                        newRun.PrependChild(GetRunProperties());
                        paragraph.AppendChild(newRun);
                    }
                    
                    // paragraph.AppendChild(newRun);
            
                    // InsertTextWithStyle(paragraph, text, runProperties);
                    // paragraph.Append(new Run(new Text(text)));
                }
            }
            #endregion

            resultDoc.Save();

            resultFilePaths.Add(resultFilePath);

            fileNameIndex++;
        }

        return resultFilePaths;
    }

    private RunProperties GetRunProperties()
    {
        var font = "ISOCPEUR";
        var runProperties = new RunProperties(new RunFonts { Ascii = font,
            HighAnsi = font,
            ComplexScript = font,
            EastAsia = font,
            Hint = FontTypeHintValues.Default,
                        
        });

        return runProperties;
    }
    
    private string? GetMostCommonFont()
    {
        // using var wordDocument = WordprocessingDocument.Open(filePath, false);
        var fonts = new List<string>();
        var body = _doc.MainDocumentPart.Document.Body;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                var runProperties = run.RunProperties;
                if (runProperties != null)
                {
                    var fontSize = runProperties.FontSize;
                    if (fontSize != null)
                    {
                        fonts.Add(fontSize.Val.Value.ToString());
                    }
                }
            }
        }

        return fonts.GroupBy(f => f).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
    }

    private void CreateResultDoc(string resultFilePath)
    {
        using WordprocessingDocument resultDoc = WordprocessingDocument
            .Create(resultFilePath, WordprocessingDocumentType.Document);
        //
        // OpenXmlElement templateBody = _doc.MainDocumentPart?.Document.Body?.CloneNode(deep: true) 
        //                               ?? throw new Exception("The body of the word document is missing.");
        //
        // resultDoc.AddMainDocumentPart();
        // resultDoc.MainDocumentPart!.Document = new Document(templateBody);
        // resultDoc.MainDocumentPart.Document.Save();
        
        foreach (var part in _doc.Parts)
            resultDoc.AddPart(part.OpenXmlPart, part.RelationshipId);
            
        resultDoc.Save();
    }

    private void InsertTextWithStyle(Paragraph paragraph, string text, RunProperties runProperties)
    {
        var run = new Run();
        var textElement = new Text(text);

        if (runProperties != null)
        {
            run.Append(runProperties.CloneNode(true));
        }

        run.Append(textElement);
        paragraph.Append(run);
    }

    private RunProperties FindMostCommonStyle(WordprocessingDocument doc)
    {
        var styleCount = new Dictionary<string, int>();

        foreach (var run in doc.MainDocumentPart.Document.Body.Descendants<Run>())
        {
            var runProperties = run.RunProperties?.CloneNode(true);
            if (runProperties != null)
            {
                var styleHash = runProperties.OuterXml;
                if (styleCount.ContainsKey(styleHash))
                {
                    styleCount[styleHash]++;
                }
                else
                {
                    styleCount[styleHash] = 1;
                }
            }
        }

        var mostCommonStyleHash = styleCount.OrderByDescending(x => x.Value).FirstOrDefault().Key;
        var mostCommonStyle = doc.MainDocumentPart.Document.Body.Descendants<Run>()
            .FirstOrDefault(r => r.RunProperties?.OuterXml == mostCommonStyleHash)?.RunProperties;

        return mostCommonStyle?.CloneNode(true) as RunProperties;
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