using DotNetEnv;
using PGS.TemplatePlaceholderBot.Exceptions;

namespace PGS.TemplatePlaceholderBot.Helpers;

/// <summary>
///     Assistant for managing environment variables.
/// </summary>
public static class EnvironmentHelper
{
    /// <summary>
    ///     Load environment variables.
    /// </summary>
    public static void SetEnvironmentVariables()
    {
        string pathToEnvFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        IEnumerable<KeyValuePair<string, string>> envVars = Env.Load(pathToEnvFile);

        foreach (KeyValuePair<string, string> envVar in envVars)
        {
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
        }
    }

    /// <summary>
    ///     Get bot token from environment variable.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="EnvironmentVariableNotFoundException"></exception>
    public static string GetBotToken()
    {
        return Environment.GetEnvironmentVariable("TOKEN")
               ?? throw new EnvironmentVariableNotFoundException("Bot token not set.");
    }

    public static string GetVolumePath()
    {
        return Environment.GetEnvironmentVariable("VOLUME_PATH")
               ?? throw new EnvironmentVariableNotFoundException("The path to the volume is not set.");
    }

    public static string GetTemplatesVolumePath()
    {
        return Environment.GetEnvironmentVariable("VOLUME_TEMPLATES_PATH")
               ?? throw new EnvironmentVariableNotFoundException("The path to the volume templates is not set.");
    }
}