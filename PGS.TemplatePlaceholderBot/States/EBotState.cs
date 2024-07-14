namespace PGS.TemplatePlaceholderBot.States;

public enum EBotState : byte
{
    Default,
    WaitingTemplateNumberForDelete,
    WaitingTemplateNumberForChoice,
    
    WaitingTemplateForCreatingExcel,
}