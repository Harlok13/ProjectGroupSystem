using Serilog;

namespace PGS.TemplatePlaceholderBot.Helpers;

public static class FileDeleteHelper
{
    public static void DeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
            
            Log.Information("{FilePath} deleted successfully.", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            if (ex.StackTrace is not null)
                Log.Error(ex.StackTrace);
        }
    }

    public static void DeleteFiles(IEnumerable<string> filePaths)
    {
        try
        {
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
                
                Log.Information("{FilePath} deleted successfully.", filePath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            if (ex.StackTrace is not null)
                Log.Error(ex.StackTrace);
        }
    }
}