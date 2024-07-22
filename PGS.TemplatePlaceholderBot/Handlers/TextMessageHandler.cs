using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Enums;
using PGS.TemplatePlaceholderBot.Exceptions;
using PGS.TemplatePlaceholderBot.Handlers.Base;
using PGS.TemplatePlaceholderBot.Keyboards;
using PGS.TemplatePlaceholderBot.States;
using PGS.TemplatePlaceholderBot.Storage;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Handlers;

public class TextMessageHandler(IMemoryCache _cache, IFileStorage _storage) : UpdateHandlerBase, IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is not { } message) return;
        if (update.Message.Text is not { } text) return;

        try
        {
            User user = _cache.GetUser(message.Chat.Id);
            switch (user.StateMachine.State)
            {
                case EBotState.Default:
                    Log.Information("Text message received: {Text}", message.Text);

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Добро пожаловать в заполнитель шаблонов.",
                        replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.MainMenu),
                        cancellationToken: cT);
                    break;
                
                case EBotState.WaitingTemplateNumberForChoice:
                    if (!int.TryParse(text, out int templateIndex))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Я ожидаю номер шаблона, Вы должны отправить число.",
                            replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
                            cancellationToken: cT);
                        
                        return;
                    }

                    // subtract one, because the sequence number of the templates starts from 1
                    string templateName = _cache.SetCurrentTemplate(templateIndex - 1);

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Текущий шаблон изменен на \"{templateName}\"",
                        cancellationToken: cT);

                    await user.StateMachine.FireAsync(EBotTrigger.Cancel);
                    _cache.UpdateUser(message.Chat.Id, user);
                    
                    Log.Information("The current template has been changed to \"{TemplateName}\"", templateName);
                    break;
                
                case EBotState.WaitingTemplateNumberForDelete:
                    if (!int.TryParse(text, out templateIndex))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: "Я ожидаю номер шаблона, Вы должны отправить число.",
                            replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
                            cancellationToken: cT);

                        return;
                    }
                    
                    // subtract one, because the sequence number of the templates starts from 1
                    templateName = _storage.RemoveTemplate(templateIndex - 1)
                        .Split(Path.DirectorySeparatorChar)[^1];

                    if (_cache.GetCurrentTemplateName() == templateName)
                    {
                        string? newCurrentTemplate = _storage.GetFirstTemplateName();
                        _cache.SetCurrentTemplate(newCurrentTemplate ?? "");

                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: $"Текущий шаблон изменен на \"{newCurrentTemplate}\"",
                            cancellationToken: cT);
                        
                        Log.Information("The current template has been changed to \"{TemplateName}\"", templateName);
                    }
                    
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Шаблон \"{templateName}\" успешно удален.",
                        cancellationToken: cT);
                    
                    await user.StateMachine.FireAsync(EBotTrigger.Cancel);
                    _cache.UpdateUser(message.Chat.Id, user);
                    
                    Log.Information("Template \"{TemplateName}\" successfully deleted.", templateName);
                    break;
            }
        }
        catch (IncorrectSelectedTemplateNumberException ex)
        {
            // the user entered an incorrect template number, the error level is not required
            Log.Information(ex.Message);
            
            // await botClient.SendTextMessageAsync(
            //     chatId: message.Chat.Id,
            //     text: "Шаблона с таким номером нет, введите другое число.",
            //     replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
            //     cancellationToken: cT);
        }
    }
}