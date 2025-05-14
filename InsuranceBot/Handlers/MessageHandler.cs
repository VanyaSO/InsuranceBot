using InsuranceBot.Exentions;
using InsuranceBot.Services;
using Telegram.Bot.Types;

namespace InsuranceBot.Handlers;

public class MessageHandler
{
    private readonly RegistrationProcessHandler _registrationHandler;
    private readonly RegistrationService _registrationService;
    private readonly CommandHandler _commandHandler;

    public MessageHandler(RegistrationProcessHandler registrationHandler, RegistrationService registrationService, CommandHandler commandHandler)
    {
        _registrationHandler = registrationHandler;
        _registrationService = registrationService;
        _commandHandler = commandHandler;
    }
    
    public async Task HandleMessageAsync(Message message)
    {
        string text = message.Text ?? string.Empty;
        long userId = message.From.Id;

        // Determine the type of message and forward it to the corresponding handler
        if (text.StartsWith('/'))
            await _commandHandler.HandleCommandAsync(message);
        else if (text.StartsWithEmoji())
            await _registrationHandler.HandleButtonAsync(message);
        else if (_registrationService.IsExpectingUploadPhoto(userId))
            await _registrationHandler.HandleUploadPhotoAsync(message);
    }
}