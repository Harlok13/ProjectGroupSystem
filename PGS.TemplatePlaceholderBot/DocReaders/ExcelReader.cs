using Aspose.Cells;

namespace PGS.TemplatePlaceholderBot.DocReaders;

public class ExcelReader : IDisposable
{
    private Workbook _workbook = null!;
    private string _filePath;
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
    
    public List<string> CreateAndFillDocs(string templatePath)
    {
        Worksheet sheet = _workbook.Worksheets[0];
        int skipHeaderRow = 1;

        _resultFilePaths = new List<string>(sheet.Cells.MaxRow - skipHeaderRow);
        
        using WordReader wordReader = new WordReader(templatePath);
        List<string> keywords = wordReader.GetKeywords();
        
        for (int rowIndex = skipHeaderRow; rowIndex <= sheet.Cells.MaxRow; rowIndex++)
        {
            Row row = sheet.Cells.Rows[rowIndex];

            Dictionary<string, string> keyValuePair = FillKeyValuePair(keywords, row);
            
            string resultFilePath = wordReader.CreateWordAndFillByTemplate(keyValuePair);
            _resultFilePaths.Add(resultFilePath);
        }

        return _resultFilePaths;
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
    
    public void Dispose()
    {
        _workbook.Dispose();
        GC.SuppressFinalize(this);
    }
    
    private Dictionary<string, string> FillKeyValuePair(List<string> keywords, Row row)
    {
        Dictionary<string, string> keyValuePair = new();
            
        for (int cellIndex = 0; cellIndex < row.Cast<Cell>().Count(); cellIndex++)
        {
            keyValuePair.Add(keywords[cellIndex], row[cellIndex].Value.ToString() ?? "");
        }

        return keyValuePair;
    }

}