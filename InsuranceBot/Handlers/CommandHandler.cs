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
        string? welcomeMessage = await _openAiService.GenerateMessageAsync($"Say hello, say that you are a bot that will help you get car insurance (not figure it out, not find it, but get it), and ask them to press the button \"{ButtonProcess.Start.GetDescription()}\" to start the process of getting car insurance. Use 0 emoji");

        await _botClient.SendMessage(
            message.Chat.Id,
            welcomeMessage ?? $"Hello! I'm an insurance bot.\nI'll help you get car insurance quickly and easily. Click '{ButtonProcess.Start.GetDescription()}' to begin.",
            replyMarkup: keyboard
        );
    }
}