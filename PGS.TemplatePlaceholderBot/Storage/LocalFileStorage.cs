using PGS.TemplatePlaceholderBot.Exceptions;
using PGS.TemplatePlaceholderBot.Helpers;

namespace PGS.TemplatePlaceholderBot.Storage;

public class LocalFileStorage : IFileStorage
{
    public IEnumerable<string> GetTemplateNames()
    {
        throw new NotImplementedException();
    }

    public string? GetFirstTemplateName()
    {
        string? templateName = Directory.GetFiles(EnvironmentHelper.GetTemplatesVolumePath())
            .FirstOrDefault()
            ?.Split(Path.DirectorySeparatorChar)[^1];

        return templateName;
    }

    public string GetTemplatePathByIndex(int index)
    {
        List<string> files = Directory.GetFiles(EnvironmentHelper.GetTemplatesVolumePath())
            .ToList();

        if (files.Count > index && index >= 0)
        {
            return files[index];
        }

        throw new IncorrectSelectedTemplateNumberException($"There is no file with this index - {index}");
    }
    
    public string GetTemplateNameByIndex(int index)
    {
        return GetTemplatePathByIndex(index).Split(Path.DirectorySeparatorChar)[^1];
    }

    public string RemoveTemplate(int index)
    {
        string templatePath = GetTemplatePathByIndex(index);
        
        FileDeleteHelper.DeleteFile(templatePath);

        return templatePath;
    }

    public string? GetTemplatePathByName(string templateName)
    {
        string? templatePath = Directory
            .GetFiles(EnvironmentHelper.GetTemplatesVolumePath())
            .FirstOrDefault(t => t.Contains(templateName));

        return templatePath;
    }

    public int GetTemplatesCount()
    {
        int templatesCount = Directory
            .GetFiles(EnvironmentHelper.GetTemplatesVolumePath())
            .Length;

        return templatesCount;
    }
}