using PGS.TemplatePlaceholderBot.Models;

namespace PGS.TemplatePlaceholderBot.Cache;

public interface IMemoryCache
{
    /// <summary>
    ///     Get the name of the current template.
    /// </summary>
    /// <returns>Template name with extension.</returns>
    string? GetCurrentTemplateName();

    string SetCurrentTemplate(int templateIndex);
    
    void SetCurrentTemplate(string templateName);

    User GetUser(long chatId);

    void UpdateUser(long chatId, User user);
}