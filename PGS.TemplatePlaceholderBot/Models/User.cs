using PGS.TemplatePlaceholderBot.States;
using Serilog;
using Stateless;
using Telegram.Bot.Types.ReplyMarkups;

namespace PGS.TemplatePlaceholderBot.Models;

public class User
{
    public long ChatId { get; set; }
    public StateMachine<EBotState, EBotTrigger> StateMachine { get; set; }

    private Dictionary<string, object?> _incorrectStateActionData = new()
    {
        ["message"] = "",
        ["replyMarkup"] = null
    };

    public bool IsIncorrectStateAction { get; private set; } = false;

    public User(long chatId)
    {
        ChatId = chatId;
        StateMachine = new StateMachine<EBotState, EBotTrigger>(EBotState.Default);

        StateMachine.Configure(EBotState.Default)
            .OnEntry(() => Log.Debug("State changed to \"{State}\"", EBotState.Default.ToString()))
            .OnEntry(() => Log.Debug("End state \"{State}\".", EBotState.Default.ToString()))
            .Permit(EBotTrigger.EnterTemplateNumberForDelete, EBotState.WaitingTemplateNumberForDelete)
            .Permit(EBotTrigger.EnterTemplateNumberForChoice, EBotState.WaitingTemplateNumberForChoice);

        StateMachine.Configure(EBotState.WaitingTemplateNumberForChoice)
            .OnEntry(() => Log.Debug("State changed to \"{State}\"", EBotState.WaitingTemplateNumberForChoice.ToString()))
            .OnEntry(() => Log.Debug("End state \"{State}\".", EBotState.WaitingTemplateNumberForChoice.ToString()))
            .Permit(EBotTrigger.Cancel, EBotState.Default);

        StateMachine.Configure(EBotState.WaitingTemplateNumberForDelete)
            .OnEntry(() => Log.Debug("State changed to \"{State}\"", EBotState.WaitingTemplateNumberForDelete.ToString()))
            .OnEntry(() => Log.Debug("End state \"{State}\".", EBotState.WaitingTemplateNumberForDelete.ToString()))
            .Permit(EBotTrigger.Cancel, EBotState.Default);
        
        StateMachine.Configure(EBotState.WaitingTemplateForCreatingExcel)
            .OnEntry(() => Log.Debug("State changed to \"{State}\"", EBotState.WaitingTemplateForCreatingExcel.ToString()))
            .OnEntry(() => Log.Debug("End state \"{State}\".", EBotState.WaitingTemplateForCreatingExcel.ToString()))
            .Permit(EBotTrigger.Cancel, EBotState.Default);
    }

    public void FillIncorrectStateActionData(string message, InlineKeyboardMarkup? inlineKeyboard = null)
    {
        _incorrectStateActionData["message"] = message;
        _incorrectStateActionData["replyMarkup"] = inlineKeyboard;

        IsIncorrectStateAction = true;
    }

    public (string? message, InlineKeyboardMarkup? inlineKeyboard) GetIncorrectStateActionData()
    {
        string? message = _incorrectStateActionData["message"] as string;
        InlineKeyboardMarkup? inlineKeyboard = _incorrectStateActionData["replyMarkup"] as InlineKeyboardMarkup;

        ResetIncorrectStateData();
        
        return (message, inlineKeyboard);
    }

    private void ResetIncorrectStateData()
    {
        _incorrectStateActionData = new Dictionary<string, object?>();
        IsIncorrectStateAction = false;
    }
}