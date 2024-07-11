namespace TelegramBot;

public interface IWordReader
{
    List<string> GetKeywords(string filePath);
    Guid FillWord(string templatePath, Dictionary<string, string> keywords);
}