using dotenv.net;
using InsuranceBot.Handlers;
using InsuranceBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InsuranceBot;

public class Program
{
    private static IServiceProvider _serviceProvider;

    static async Task Main()
    {
        // Load variables from .env file
        string envFilePath = Path.Combine(AppContext.BaseDirectory, "../../../../.env");
        DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { envFilePath }, probeForEnv: false));

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        
        // Get api key and create bot client
        string? token = Environment.GetEnvironmentVariable("TELEGRAM_API_KEY");
        if (string.IsNullOrEmpty(token))
        {
            var tempProvider = services.BuildServiceProvider();
            var tempLogger = tempProvider.GetRequiredService<ILogger<Program>>();
            tempLogger.LogError("Telegram API Key is missing");
            return;
        }
        
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
        services.AddSingleton<RegistrationProcessHandler>();
        services.AddSingleton<CommandHandler>();
        services.AddSingleton<MessageHandler>();
        services.AddSingleton<RegistrationService>();
        services.AddSingleton<MindeeService>();
        services.AddSingleton<OpenAiService>();
        services.AddSingleton<FileService>();
        
        _serviceProvider = services.BuildServiceProvider();


        var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
        var botClient = _serviceProvider.GetRequiredService<ITelegramBotClient>();
        using var cts = new CancellationTokenSource();
        
        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );

        logger.LogInformation("Start listening for updates.");
        
        try
        {
            await Task.Delay(-1, cts.Token);
            Console.ReadKey();
        }
        catch (TaskCanceledException ex)
        {
            logger.LogInformation(ex, "App stopped.");
        }
    }

    static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
            return;

        var messageHandler = _serviceProvider.GetRequiredService<MessageHandler>();
        await messageHandler.HandleMessageAsync(message);
    }
    
    static async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
    
        if (exception is ApiRequestException apiEx && apiEx.ErrorCode == 403)
        {
            logger.LogWarning($"Bot blocked by user: {apiEx.Message}");
            return;
        }
    
        logger.LogError(exception, "Polling error occurred");
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
}