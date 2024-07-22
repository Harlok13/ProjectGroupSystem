using PGS.TemplatePlaceholderBot.Models;
using PGS.TemplatePlaceholderBot.Storage;

namespace PGS.TemplatePlaceholderBot.Cache;

public class MemoryCache : IMemoryCache
{
    private readonly Dictionary<string, object> _cache = new();

    private const string CurrentTemplateKey = "key_currentTemplate";

    private readonly IFileStorage _storage;

    public MemoryCache(IFileStorage storage)
    {
        _storage = storage;
        
        string templateName = _storage.GetFirstTemplateName() ?? "";

        SetCurrentTemplate(templateName);
    }
    
    /// <inheritdoc/>
    public string? GetCurrentTemplateName()
    {
        return _cache.GetValueOrDefault(CurrentTemplateKey) as string;
    }

    public string SetCurrentTemplate(int templateIndex)
    {
        string templateName = _storage.GetTemplateNameByIndex(templateIndex);
        _cache[CurrentTemplateKey] = templateName;

        return templateName;
    }

    public void SetCurrentTemplate(string templateName)
    {
        _cache[CurrentTemplateKey] = templateName;
    }
    
    public User GetUser(long chatId)
    {
        string key = $"key_{chatId}";

        if (_cache.GetValueOrDefault(key) is User user) 
            return user;
        
        user = new User(chatId);
        _cache.Add(key, user);

        return user;
    }

    public void UpdateUser(long chatId, User user)
    {
        _cache[$"key_{chatId}"] = user;
    }
}