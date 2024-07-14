namespace PGS.TemplatePlaceholderBot.Storage;

public interface IFileStorage
{
    IEnumerable<string> GetTemplateNames();

    string? GetFirstTemplateName();

    string GetTemplateNameByIndex(int index);
    
    string RemoveTemplate(int index);
    
    string? GetTemplatePathByName(string templateName);
}