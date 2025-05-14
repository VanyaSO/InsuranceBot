using InsuranceBot.Enums;
using InsuranceBot.Exentions;
using InsuranceBot.Helpers;
using InsuranceBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InsuranceBot.Handlers;

public class CommandHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly OpenAiService _openAiService;

    public CommandHandler(ITelegramBotClient botClient, OpenAiService openAiService)
    {
        _botClient = botClient;
        _openAiService = openAiService;
    }
    
    public async Task HandleCommandAsync(Message message)
    {
        if (message.Text != "/start")
            return;

        var keyboard = KeyboardHelper.MainMenuKeyboard();
        string? welcomeMessage = await _openAiService.GenerateMessageAsync($"Say hello (no emoji), tell them you are a bot that will help them create car insurance, and ask them to click the '{ButtonProcess.Start.GetDescription()}' button to begin the insurance process.");

        await _botClient.SendMessage(
            message.Chat.Id,
            welcomeMessage ?? $"Hello! I'm an insurance bot.\nI'll help you get car insurance quickly and easily. Click '{ButtonProcess.Start.GetDescription()}' to begin.",
            replyMarkup: keyboard
        );
    }
}