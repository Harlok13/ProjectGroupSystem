using Aspose.Cells;
using DocumentFormat.OpenXml.Drawing;
using Serilog;
using Picture = Aspose.Cells.Drawing.Picture;

namespace PGS.TemplatePlaceholderBot.DocReaders;

public class ExcelReader : IDisposable
{
    private Workbook _workbook = null!;
    private string _filePath = null!;

    private ExcelReader() { }
    
    public static ExcelReader Open(string filePath)
    {
        ExcelReader excelReader = new();
        excelReader._filePath = filePath;
        excelReader._workbook = new Workbook(filePath);

        return excelReader;
    }

    public static ExcelReader Create(string filePath, SaveFormat saveFormat)
    {
        ExcelReader excelReader = new();
        excelReader._filePath = filePath;
        excelReader._workbook = new Workbook();
        excelReader._workbook.Save(filePath, saveFormat);

        return excelReader;
    }
    
    public List<Dictionary<string, object>> GetKeyValuePairs(string templatePath)
    {
        Worksheet sheet = _workbook.Worksheets[0];
        int skipHeaderRow = 5;
        int skipColumn = 3;

        List<Dictionary<string, object>> keyValuePairsList = new (sheet.Cells.MaxColumn - skipColumn);
        
        using WordReader wordReader = new WordReader(templatePath);
        
        int imageIndex = 1;
        int columnIndex = skipColumn;
        
        while (true)
        {
            var column = columnIndex >= 26 
                ? "A" 
                : ((char)('A' + columnIndex)).ToString();
            
            int tempIndex = columnIndex;
            if (columnIndex >= 26)
            {
                while (tempIndex >= 26)
                {
                    tempIndex -= 26;
                    column += (char)('A' + columnIndex % 26);
                }
            }

            try
            {
                if (string.IsNullOrWhiteSpace(sheet.Cells[$"{column}2"].StringValue))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error with column name");
                Log.Error(ex.Message);
                if(ex.StackTrace is not null)
                    Log.Error(ex.StackTrace);
                
                break;
            }
            
            if (string.IsNullOrWhiteSpace(sheet.Cells[$"{column}7"].StringValue))
            {
                columnIndex++;
                continue;
            }
                // break;

            // var keywords = wordReader.GetKeywords();
            // if (!string.IsNullOrWhiteSpace(keywords[10]))
            // {
            //     columnIndex++;
            // }
            
            Dictionary<string, object> keyValuePairs = FillKeyValuePair(column, skipHeaderRow, imageIndex, sheet, out bool hasImage);
            keyValuePairsList.Add(keyValuePairs);

            columnIndex++;
            
            if (hasImage)
                imageIndex++;
        
            // if (columnIndex > 10)
            //     break;
        }
        
            
        return keyValuePairsList;
    }
    
    [Obsolete]
    public void CreateExcelByTemplate(string? templatePath)
    {
        ArgumentNullException.ThrowIfNull(templatePath);
        
        Worksheet sheet = _workbook.Worksheets[0];

        using WordReader wordReader = new(templatePath);
        List<string> keywords = wordReader.GetKeywords();


        for (int index = 0; index < keywords.Count; index++)
        {
            char column = (char)('A' + index);
            int row = index + 1;  // Start from the first row

            if (index > 0)
            {
                row -= index;
            }

            sheet.Cells[$"{column}{row}"].PutValue(keywords[index]);
        }
        
        _workbook.Save(_filePath);
    }

    private Dictionary<string, object> FillKeyValuePair(string column, int startRowIndex, int imageIndex, Worksheet sheet, out bool hasImage)
    {
        Dictionary<string, object> keyValuePair = new();
        
        hasImage = false;
        for (int rowIndex = 0; rowIndex <= 19 - startRowIndex; rowIndex++)
        {
            string columnName = $"{column}{rowIndex + startRowIndex}";
            string key = sheet.Cells[$"A{rowIndex + startRowIndex}"].StringValue ?? "";
            
            Cell cell = sheet.Cells[columnName];

            const string rowIndexOfPicture = "11";
            if (columnName.EndsWith(rowIndexOfPicture) && string.IsNullOrWhiteSpace(cell.StringValue))
            {
                try
                {
                    Picture pic = sheet.Pictures[imageIndex];
                    keyValuePair.Add(key, pic ?? (object)"");
                    hasImage = true;

                    continue;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    if (ex.StackTrace is not null)
                        Log.Error(ex.StackTrace);
                    
                    Log.Information("Failed to attach image.");
                    continue;
                }
            }

            keyValuePair[key] = cell.StringValue;
            // keyValuePair.Add(key, cell.StringValue);
        }
        
        // todo create class TemplatePairs with property FileName
        const string rowIndexOfFileName = "7";
        string fileName = sheet.Cells[$"{column}{rowIndexOfFileName}"].StringValue;
        fileName = fileName.Replace("/", ":");  // escaping slash
        
        keyValuePair.Add("FileName", fileName);

        return keyValuePair;
    }
    
    #region DisposePattern

    private bool _disposed;

    ~ExcelReader()
    {
        Dispose(false);
    }
    
    public void Dispose()
    {
        _workbook.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _workbook.Dispose();
        }
        
        _disposed = true;
    }

    #endregion
}