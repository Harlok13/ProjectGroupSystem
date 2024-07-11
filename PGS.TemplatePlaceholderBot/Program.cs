using PGS.TemplatePlaceholderBot;
using Telegram.Bot;
using Telegram.Bot.Polling;

var botClient = new TelegramBotClient("6188341346:AAGE65eiFiTEpM7DC9xzDU6e68EoQu2xOdg");

botClient.StartReceiving(
    updateHandler: Hadlers.HandleUpdateAsync,
    pollingErrorHandler: Hadlers.HandlePollingErrorAsync,
    receiverOptions: new ReceiverOptions
    {
        AllowedUpdates = [] // receive all update types
    });
Console.WriteLine($"StartBot - {botClient.BotId}");

Console.Read();
