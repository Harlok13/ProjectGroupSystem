using Aspose.Cells;
using Aspose.Cells.Drawing;
using Serilog;

namespace PGS.TemplatePlaceholderBot.DocReaders;

public class ExcelReader : IDisposable
{
    private Workbook _workbook = null!;
    private string _filePath = null!;
    private List<string> _resultFilePaths = null!;

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
    
    // public List<string> CreateAndFillDocs(string templatePath)
    // {
    //     Worksheet sheet = _workbook.Worksheets[0];
    //     // int skipHeaderRow = 1;
    //     int skipHeaderRow = 3;
    //
    //     _resultFilePaths = new List<string>(sheet.Cells.MaxRow - skipHeaderRow);
    //     
    //     using WordReader wordReader = new WordReader(templatePath);
    //     List<string> keywords = wordReader.GetKeywords();
    //     
    //     for (int rowIndex = skipHeaderRow; rowIndex <= sheet.Cells.MaxRow; rowIndex++)
    //     {
    //         Row row = sheet.Cells.Rows[rowIndex];
    //
    //         Dictionary<string, string> keyValuePair = FillKeyValuePair(keywords, row);
    //         
    //         string resultFilePath = wordReader.CreateWordAndFillByTemplate(keyValuePair);
    //         _resultFilePaths.Add(resultFilePath);
    //     }
    //
    //     return _resultFilePaths;
    // }
    
    public List<Dictionary<string, object>> GetKeyValuePairs(string templatePath)
    {
        Worksheet sheet = _workbook.Worksheets[0];
        int skipHeaderRow = 5;
        int skipColumn = 3;

        // _resultFilePaths = new List<string>(sheet.Cells.MaxColumn - skipColumn);
        List<Dictionary<string, object>> keyValuePairsList = new (sheet.Cells.MaxColumn - skipColumn);
        
        using WordReader wordReader = new WordReader(templatePath);
        // List<string> keywords = wordReader.GetKeywords();
        int imageIndex = 1;
        int columnIndex = skipColumn;
        // for (int columnIndex = skipColumn; columnIndex <= sheet.Cells.MaxColumn; columnIndex++)
        while (true)
        {
            // Cell cell = sheet.Cells[rowIndex];
            char column = (char)('A' + columnIndex);
            if (string.IsNullOrWhiteSpace(sheet.Cells[$"{column}5"].StringValue))
                break;

            Dictionary<string, object> keyValuePairs = FillKeyValuePair(column, skipHeaderRow, imageIndex, sheet);
            keyValuePairsList.Add(keyValuePairs);
            
            // string resultFilePath = wordReader.CreateWordAndFillByTemplate(keyValuePair);
            // string resultFilePath = "wordReader.CreateWordAndFillByTemplate(keyValuePair);";
            // _resultFilePaths.Add(resultFilePath);
            imageIndex++;
            columnIndex++;
        }

        return keyValuePairsList;
        // return _resultFilePaths;
    }
    
    public void CreateExcelByTemplate(string? templatePath)
    {
        ArgumentNullException.ThrowIfNull(templatePath);
        
        Worksheet sheet = _workbook.Worksheets[0];

        using WordReader wordReader = new(templatePath);
        List<string> keywords = wordReader.GetKeywords();


        for (int index = 0; index < keywords.Count; index++)
        {
            // sheet.Cells[$"{(char)('A' + index)}{index + 1}"].PutValue(keywords[index]);
            char column = (char)('A' + index);
            int row = index + 1; // Start from the first row

            // If the keyword is not the first one, shift its row by its index
            if (index > 0)
            {
                row -= index;
            }

            sheet.Cells[$"{column}{row}"].PutValue(keywords[index]);
        }
        
        _workbook.Save(_filePath);
    }
    
    // private Dictionary<string, string> FillKeyValuePair(List<string> keywords, Row row)
    // {
    //     Dictionary<string, string> keyValuePair = new();
    //         
    //     for (int cellIndex = 0; cellIndex < row.Cast<Cell>().Count(); cellIndex++)
    //     {
    //         keyValuePair.Add(keywords[cellIndex], row[cellIndex].Value.ToString() ?? "");
    //     }
    //
    //     return keyValuePair;
    // }

    private Dictionary<string, object> FillKeyValuePair(char column, int startRowIndex, int imageIndex, Worksheet sheet)
    {
        Dictionary<string, object> keyValuePair = new();
        
        for (int rowIndex = 0; rowIndex <= 19 - startRowIndex; rowIndex++)
        {
            string columnName = $"{column}{rowIndex + startRowIndex}";
            string key = sheet.Cells[$"A{rowIndex + startRowIndex}"].StringValue;
            Cell cell = sheet.Cells[columnName];

            if (columnName.EndsWith("11"))
            {
                try
                {
                    Picture pic = sheet.Pictures[imageIndex];
                    keyValuePair.Add(key, pic ?? (object)"");

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
            keyValuePair.Add(key, cell.StringValue);
        }
        
        // todo create class TemplatePairs with property FileName
        string fileName = sheet.Cells[$"{column}7"].StringValue;
        fileName = fileName.Replace("/", ":");
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