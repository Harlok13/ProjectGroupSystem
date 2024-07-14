using PGS.TemplatePlaceholderBot;
using PGS.TemplatePlaceholderBot.Helpers;
using Serilog;
using Telegram.Bot;

EnvironmentHelper.SetEnvironmentVariables();

TelegramBotClient botClient = new TelegramBotWrapper()
    .CreateBotClient();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("StartBot - {BotId}", botClient.BotId);

Console.Read();