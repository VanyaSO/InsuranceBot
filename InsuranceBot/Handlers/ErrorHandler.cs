using InsuranceBot.Helpers;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace InsuranceBot.Handlers;

public static class ErrorHandler
{
    public static async Task HandleAsync<T>(ILogger<T> logger, Func<Task> action, string errorMessage, ITelegramBotClient botClient, long chatId)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage);
            await botClient.SendMessage(chatId, "An error occurred. Please try again.", replyMarkup: KeyboardHelper.MainMenuKeyboard());
        }
    }
}