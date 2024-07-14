using System.Text;
using Aspose.Cells;
using PGS.TemplatePlaceholderBot.Cache;
using PGS.TemplatePlaceholderBot.Constants;
using PGS.TemplatePlaceholderBot.DocReaders;
using PGS.TemplatePlaceholderBot.Enums;
using PGS.TemplatePlaceholderBot.Helpers;
using PGS.TemplatePlaceholderBot.Keyboards;
using PGS.TemplatePlaceholderBot.States;
using PGS.TemplatePlaceholderBot.Storage;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using File = System.IO.File;
using User = PGS.TemplatePlaceholderBot.Models.User;

namespace PGS.TemplatePlaceholderBot.Handlers.Callbacks;

public class TemplateMenuCallbackHandler(IMemoryCache _cache, IFileStorage _storage) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cT)
    {
        if (update.Message is { }) return;
        if (update.CallbackQuery is not { } cb) return;
        if (cb.Message is not { } message) return;

        switch (cb.Data)
        {
            case CallbackConstants.GetTemplates:
                await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: GetFormattedTemplatesList(),
                    replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.TemplateActions),
                    cancellationToken: cT);
                
                break;
            
            case CallbackConstants.ChoiceHowCreateExcelByTemplate:
                string? currentTemplate = _cache.GetCurrentTemplateName();
                    
                await botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: $"Выберите как Вы хотите создать excel.\nТекущий шаблон: {currentTemplate}",
                    replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.CreateExcelByTemplateActions),
                    cancellationToken: cT);
                
                break;
            
            case CallbackConstants.ChoiceTemplate:
                User user = _cache.GetUser(message.Chat.Id);
                await user.StateMachine.FireAsync(EBotTrigger.EnterTemplateNumberForChoice);
                
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Для выбора шаблона отправьте его порядковый номер.",
                    replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
                    cancellationToken: cT);
                
                _cache.UpdateUser(message.Chat.Id, user);
                
                break;
            
            case CallbackConstants.RemoveTemplate:
                user = _cache.GetUser(message.Chat.Id);
                await user.StateMachine.FireAsync(EBotTrigger.EnterTemplateNumberForDelete);
                
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Для удаления шаблона отправьте его порядковый номер.",
                    replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
                    cancellationToken: cT);
                
                _cache.UpdateUser(message.Chat.Id, user);

                break;
            
            case CallbackConstants.DownloadNewTemplateForGeneratingExcel:
                user = _cache.GetUser(message.Chat.Id);
                await user.StateMachine.FireAsync(EBotTrigger.EnterTemplateForCreateExcel);
                
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Загрузите новый шаблон для создания excel файла по нему.",
                    replyMarkup: InlineKeyboardBuilder.Build(ETemplateMenuKeyboard.Cancel),
                    cancellationToken: cT);
                
                _cache.UpdateUser(message.Chat.Id, user);
                
                break;
            
            case CallbackConstants.UseCurrentTemplateForGeneratingExcel:
                string? templateName = _cache.GetCurrentTemplateName();
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "У вас нет ни одного шаблона, попробуйте загрузить шаблон и попробовать еще раз.",
                        cancellationToken: cT);

                    return;
                }

                string excelPath = GetFormattedExcelPath(templateName);
                using (ExcelReader excelReader = ExcelReader.Create(excelPath, SaveFormat.Xlsx))
                {
                    string? templatePath = _storage.GetTemplatePathByName(templateName);
                    excelReader.CreateExcelByTemplate(templatePath);
                }

                await botClient.SendDocumentAsync(
                    chatId: message.Chat.Id,
                    document: InputFile.FromStream(File.Open(excelPath, FileMode.Open), GetFormattedExcelName(excelPath)),
                    cancellationToken: cT);
                
                FileDeleteHelper.DeleteFile(excelPath);
                
                Log.Information("An excel document \"{ExcelName}\" created using the template \"{TemplateName}\" was sent", 
                    excelPath.Split(Path.DirectorySeparatorChar)[^1], templateName);

                break;
            
            case CallbackConstants.Cancel:
                user = _cache.GetUser(message.Chat.Id);
                await user.StateMachine.FireAsync(EBotTrigger.Cancel);
                
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Действие было отменено.",
                    cancellationToken: cT);
                
                _cache.UpdateUser(message.Chat.Id, user);

                break;
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cT)
    {
        Log.Error(exception.Message);
        if(exception.StackTrace is not null)
            Log.Error(exception.StackTrace);

        return Task.CompletedTask;
    }

    private string GetFormattedTemplatesList()
    {
        string[] templatePaths = Directory.GetFiles(EnvironmentHelper.GetTemplatesVolumePath());
        string[] templateNames = GetTemplateNames(templatePaths);

        StringBuilder sb = new();
        sb.Append("Список шаблонов:\n");
        foreach (int templateIndex in Enumerable.Range(1, templatePaths.Length))
        {
            sb.Append(templateIndex).Append(".").Append("  ").Append(templateNames[templateIndex - 1]).Append("\n");
        }

        sb.AppendFormat("\nТекущий выбранный шаблон:\n{0}", _cache.GetCurrentTemplateName());

        return sb.ToString();
    }

    private string[] GetTemplateNames(string[] templatePaths) =>
        templatePaths.Select(tp => tp.Split(Path.DirectorySeparatorChar)[^1]).ToArray();
    
    private string GetFormattedExcelPath(string templateName) =>
        $"{EnvironmentHelper.GetVolumePath()}/generated_by_{templateName.Split('.')[0]}.xlsx";

    private string GetFormattedExcelName(string templatePath) =>
        templatePath.Split(Path.DirectorySeparatorChar)[^1];
}