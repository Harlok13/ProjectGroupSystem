using PGS.TemplatePlaceholderBot.Constants;
using PGS.TemplatePlaceholderBot.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PGS.TemplatePlaceholderBot.Keyboards;

public static class InlineKeyboardBuilder
{
    public static InlineKeyboardMarkup Build(ETemplateMenuKeyboard type)
    {
        return type switch
        {
            ETemplateMenuKeyboard.MainMenu => MainMenu(),
            ETemplateMenuKeyboard.TemplateActions => TemplateActions(),
            ETemplateMenuKeyboard.CreateExcelByTemplateActions => CreateExcelByTemplateActions(),
            ETemplateMenuKeyboard.Cancel => Cancel(),
            _ => throw new Exception($"Keyboard type \"{type}\" missing.")
        };
    }

    private static InlineKeyboardMarkup CreateExcelByTemplateActions()
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Использовать текущий шаблон", CallbackConstants.UseCurrentTemplateForGeneratingExcel),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Загрузить новый шаблон", CallbackConstants.DownloadNewTemplateForGeneratingExcel), 
            }
        });

        return inlineKeyboard;
    }

    private static InlineKeyboardMarkup Cancel()
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Отменить", CallbackConstants.Cancel),
            }
        });

        return inlineKeyboard;
    }

    private static InlineKeyboardMarkup TemplateActions()
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Удалить шаблон", CallbackConstants.RemoveTemplate),
                InlineKeyboardButton.WithCallbackData("Выбрать шаблон", CallbackConstants.ChoiceTemplate),
            },
            new[]
            {
                InlineKeyboardButton.WithWebApp("Управлять шаблонами",
                    new WebAppInfo() { Url = "https://chat.mistral.ai/chat" }),
                // InlineKeyboardButton.WithCallbackData("Управлять шаблонами", CallbackConstants.ChoiceTemplate),
            }
        });

        return inlineKeyboard;
    }

    private static InlineKeyboardMarkup MainMenu()
    {
        InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Открыть список шаблонов", CallbackConstants.GetTemplates),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Создать excel по шаблону", CallbackConstants.ChoiceHowCreateExcelByTemplate),
            }
        });

        return inlineKeyboard;
    }
}