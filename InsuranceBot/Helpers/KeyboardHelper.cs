using InsuranceBot.Enums;
using InsuranceBot.Exentions;
using Telegram.Bot.Types.ReplyMarkups;

namespace InsuranceBot.Helpers;

public static class KeyboardHelper
{
    public static ReplyKeyboardMarkup MainMenuKeyboard()
    {
        var keyboard = new ReplyKeyboardMarkup(new List<KeyboardButton>())
        {
            ResizeKeyboard = true
        };

        keyboard.AddButton(ButtonProcess.Start.GetDescription());
        return keyboard;
    }

    public static ReplyKeyboardMarkup CreateClearKeyboard()
    {
        return new ReplyKeyboardMarkup(new List<KeyboardButton>()) { ResizeKeyboard = true };
    }

    public static bool IsKeyboardHasButton(ReplyKeyboardMarkup keyboard, ButtonProcess button)
    {
        return keyboard.Keyboard.Any(r => r.Any(b => b.Text == button.GetDescription()));
    }
}