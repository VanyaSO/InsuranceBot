using InsuranceBot.Enums;
using InsuranceBot.Exentions;
using InsuranceBot.Helpers;
using InsuranceBot.Models;
using InsuranceBot.Models.DTO;
using InsuranceBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InsuranceBot.Handlers;

public class RegistrationProcessHandler
{
    private readonly ILogger<RegistrationProcessHandler> _logger;
    private readonly RegistrationService _registrationService;
    private readonly ITelegramBotClient _botClient;
    private readonly MindeeService _mindeeService;
    private readonly OpenAiService _openAiService;
    private readonly FileService _fileService;

    private readonly Dictionary<StepProcess, Func<Message, Task>> _stepHandlers;

    private bool _isRestart;
    private ReplyKeyboardMarkup _keyboard;

    public RegistrationProcessHandler(ILogger<RegistrationProcessHandler> logger,
        RegistrationService registrationService, ITelegramBotClient botClient, MindeeService mindeeService,
        OpenAiService openAiService, FileService fileService)
    {
        _logger = logger;
        _registrationService = registrationService;
        _botClient = botClient;
        _mindeeService = mindeeService;
        _openAiService = openAiService;
        _fileService = fileService;

        _stepHandlers = new Dictionary<StepProcess, Func<Message, Task>>
        {
            { StepProcess.Start, HandleStartStep },
            { StepProcess.ConfirmData, HandleConfirmDataStep },
            { StepProcess.CostCalculation, HandleCostCalculationStep },
            { StepProcess.IssuanceInsurancePolicy, HandleIssuanceStep },
            { StepProcess.Finish, HandleFinishStep }
        };
    }

    public async Task HandleButtonAsync(Message message)
    {
        long chatId = message.Chat.Id;
        long userId = message.From.Id;


        // Execute with error handling and logging
        await ErrorHandler.HandleAsync(_logger, async () =>
        {
            do
            {
                _isRestart = false;

                // Extract button text and initialize keyboard
                string buttonText = message.Text;
                _keyboard = new ReplyKeyboardMarkup(new List<KeyboardButton>()) { ResizeKeyboard = true };

                // Handle Cancel button press
                if (buttonText == ButtonProcess.Cancel.GetDescription())
                {
                    // Clear user registration progress
                    _registrationService.RemoveUserProgress(userId);

                    string? cancelMessage = await _openAiService.GenerateMessageAsync("The user has cancelled the registration process. Please inform them that the process has been reset and suggest starting over.");

                    // Send reset confirmation and show main menu
                    await _botClient.SendMessage(
                        chatId,
                        cancelMessage ?? "The registration process has been cancelled.",
                        replyMarkup: KeyboardHelper.MainMenuKeyboard());
                    return;
                }
                // Handle Start button press
                else if (message.Text == ButtonProcess.Start.GetDescription())
                {
                    _registrationService.RemoveUserProgress(userId);
                    _registrationService.StartRegistration(userId);
                }

                // Get current registration step
                StepProcess currentStep = _registrationService.GetCurrentStep(userId);

                // Execute step-specific handler if exists
                if (_stepHandlers.ContainsKey(currentStep))
                    await _stepHandlers[currentStep](message);
                else
                    return;

                // Get and send step message if available
                string? text = await _registrationService.GetStepMessageAsync(userId);

                if (!string.IsNullOrEmpty(text))
                    await _botClient.SendMessage(chatId, text, replyMarkup: _keyboard);
                
            } while (_isRestart);
        }, "Error processing button.", _botClient, chatId);
    }

    private Task HandleStartStep(Message message)
    {
        long userId = message.From.Id;

        // Move to next step
        _registrationService.MoveToNextStep(userId);
        _keyboard.AddButtons(ButtonProcess.Cancel.GetDescription());

        return Task.CompletedTask;
    }

    private async Task HandleConfirmDataStep(Message message)
    {
        long userId = message.From.Id;
        long chatId = message.Chat.Id;

        // Handle Yes/No confirmation data
        if (message.Text == ButtonProcess.Yes.GetDescription())
        {
            // Move to next step
            _registrationService.MoveToNextStep(userId);
        }
        else if (message.Text == ButtonProcess.No.GetDescription())
        {
            // Back to step upload photo
            _registrationService.SetStep(userId, StepProcess.IdCardUpload);

            string? tryAgainMessage = await _openAiService.GenerateMessageAsync("Tell the user to try uploading the photos again.");
            
            _keyboard.AddButtons(ButtonProcess.Cancel.GetDescription());
            await _botClient.SendMessage(
                chatId, 
                tryAgainMessage ?? "Please upload the photo again.",
                replyMarkup: new ReplyKeyboardRemove());

            _isRestart = true;
        }
    }

    private async Task HandleCostCalculationStep(Message message)
    {
        long userId = message.From.Id;
        long chatId = message.Chat.Id;

        // Handle Yes/No confirmation the calculated price
        if (message.Text == ButtonProcess.Yes.GetDescription())
        {
            // Move to issuance step
            _registrationService.MoveToNextStep(userId);
            
            await _botClient.SendMessage(chatId, "Thank you!", replyMarkup: new ReplyKeyboardRemove());
            
            _isRestart = true;
        }
        else if (message.Text == ButtonProcess.No.GetDescription())
        {
            // Reset registration
            _registrationService.RemoveUserProgress(userId);

            string? answerMessage = await _openAiService.GenerateMessageAsync($"The user did not accept the price {Constants.Insurance.DefaultCost}. Please indicate that this is the only price and reset the process.");
            
            await _botClient.SendMessage(chatId,
                answerMessage ??
                $"Sorry, {Constants.Insurance.DefaultCost} is the only price available.\nThe registration process has been reset.",
                replyMarkup: KeyboardHelper.MainMenuKeyboard());
        }
    }

    private async Task HandleIssuanceStep(Message message)
    {
        long userId = message.From.Id;
        long chatId = message.Chat.Id;

        // Get insurance details from registration service
        InsuranceDetails? details = _registrationService.GetInsuranceDetails(userId);
        if (details != null)
        {
            // Generate insurance policy text
            string text = await _openAiService.GenerateInsuranceAsync(details) ?? "Failed to create policy. Try again.";

            // Move to next step
            _registrationService.MoveToNextStep(userId);
            await _botClient.SendMessage(chatId, text, replyMarkup: KeyboardHelper.MainMenuKeyboard());

            _isRestart = true;
        }
        else
        {
            // Log error and reset process
            _logger.LogWarning("Insurance data is not available. Please try again.");

            string? answerMessage = await _openAiService.GenerateMessageAsync("Information for registration of insurance is not available. Ask to start over.");

            await _botClient.SendMessage(
                chatId, 
                answerMessage ?? "Insurance data not available. Please try again.",
                replyMarkup: KeyboardHelper.MainMenuKeyboard());

            _registrationService.RemoveUserProgress(userId);
        }
    }

    private Task HandleFinishStep(Message message)
    {
        long userId = message.From.Id;

        _registrationService.RemoveUserProgress(userId);

        return Task.CompletedTask;
    }

    // Process uploaded photos when registering insurance 
    public async Task HandleUploadPhotoAsync(Message message)
    {
        long userId = message.From.Id;
        long chatId = message.Chat.Id;

        // Check if there is active progress
        if (!_registrationService.IsUserProgressExist(userId))
        {
            string? startProcessMessage = await _openAiService.GenerateMessageAsync($"The registration process has not started. Please press {ButtonProcess.Start.GetDescription()}.");
            
            await _botClient.SendMessage(
                chatId,
                startProcessMessage ?? $"The registration process has not started. Click {ButtonProcess.Start.GetDescription()}.",
                replyMarkup: KeyboardHelper.MainMenuKeyboard());
            return;
        }

        // Checking for photo availability
        PhotoSize[]? photo = message.Photo;
        if (photo == null || photo.Length == 0)
        {
            string? noGetPhotoMessage = await _openAiService.GenerateMessageAsync("The user has not sent a photo. Ask to upload a photo.");
            
            await _botClient.SendMessage(
                chatId, 
                noGetPhotoMessage ?? "Please upload a photo.");
            return;
        }

        await ErrorHandler.HandleAsync(_logger, async () =>
        {
            _keyboard = new ReplyKeyboardMarkup(new List<KeyboardButton>()) { ResizeKeyboard = true };

            _keyboard.AddButtons(ButtonProcess.Cancel.GetDescription());

            string? processingMessage = await _openAiService.GenerateMessageAsync("The photo is being processed. Ask to wait.");
            
            await _botClient.SendMessage(
                chatId, 
                processingMessage ?? "Please wait - the photo is being processed...",
                replyMarkup: _keyboard);

            // Load the file and check the file path
            string? filePath = await DownloadAndSaveFileAsync(photo.Last().FileId);
            if (string.IsNullOrEmpty(filePath))
            {
                string? downloadErrorMessage = await _openAiService.GenerateMessageAsync("Error loading photo. Ask to try again.");
                
                await _botClient.SendMessage(
                    chatId, 
                    downloadErrorMessage ?? "Error loading photo. Try again.",
                    replyMarkup: _keyboard);
                return;
            }

            // Get insurance data
            InsuranceDetails? insurance = _registrationService.GetInsuranceDetails(userId);
            if (insurance == null)
            {
                string? noDetailsMessage = await _openAiService.GenerateMessageAsync("Information for registration of insurance is not available. Ask to start over.");
                
                await _botClient.SendMessage(
                    chatId, 
                    noDetailsMessage ?? "Error. Please start over.",
                    replyMarkup: KeyboardHelper.MainMenuKeyboard());
                
                _registrationService.RemoveUserProgress(userId);
                return;
            }

            bool processingError = false;

            // Process the document depending on the current step
            switch (_registrationService.GetCurrentStep(userId))
            {
                case StepProcess.IdCardUpload:
                {
                    IdCardDto? dto = await _mindeeService.GetIdCardDataAsync(filePath);
                    if (dto != null)
                    {
                        insurance.FullName = dto.FullName;
                        insurance.DateOfBirth = dto.DateOfBirth;
                        insurance.IdCardNumber = dto.DocumentNumber;
                        _registrationService.MoveToNextStep(userId);
                    }
                    else
                        processingError = true;

                    break;
                }
                case StepProcess.VRDFrontSideUpload:
                {
                    VRDFrontSideDto? dto = await _mindeeService.GetVrdFrontSideDataAsync(filePath);
                    if (dto != null)
                    {
                        insurance.CarNumber = dto.CarNumber;
                        _registrationService.MoveToNextStep(userId);
                    }
                    else
                        processingError = true;

                    break;
                }
                case StepProcess.VRDBackSideUpload:
                {
                    VRDBackSideDto? dto = await _mindeeService.GetVrdBackSideDataAsync(filePath);
                    if (dto != null)
                    {
                        insurance.CarBrand = dto.CarBrand;
                        insurance.CarModel = dto.CarModel;
                        insurance.VIN = dto.VIN;

                        _registrationService.MoveToNextStep(userId);
                        _keyboard.AddNewRow(
                            ButtonProcess.No.GetDescription(),
                            ButtonProcess.Yes.GetDescription());
                    }
                    else
                        processingError = true;

                    break;
                }
                default:
                {
                    processingError = true;
                    break;
                }
            }

            // Delete the file
            _fileService.DeleteFile(filePath);

            // Process the result
            if (processingError)
            {
                string? errorMessage = await _openAiService.GenerateMessageAsync("Error processing photo. Ask to try again.");
                
                await _botClient.SendMessage(
                    chatId, 
                    errorMessage ?? "Error processing photo. Please try again.",
                    replyMarkup: _keyboard);
                return;
            }

            // Get a message about the next step and send or send error
            string? nextStepMessage = await _registrationService.GetStepMessageAsync(userId);
            if (!string.IsNullOrEmpty(nextStepMessage))
            {
                await _botClient.SendMessage(
                    chatId, 
                    nextStepMessage,
                    replyMarkup: _keyboard);
            }
            else
            {
                string? stepErrorMessage = await _openAiService.GenerateMessageAsync("Something went wrong. Ask to start over.");
                
                await _botClient.SendMessage(
                    chatId, 
                    stepErrorMessage ?? "Something went wrong",
                    replyMarkup: KeyboardHelper.MainMenuKeyboard());

                _registrationService.RemoveUserProgress(userId);
            }
        }, "Photo processing error", _botClient, chatId);
    }

    // Downloads file from Telegram and save
    private async Task<string?> DownloadAndSaveFileAsync(string fileId)
    {
        string pathFilesDir = Path.Combine(AppContext.BaseDirectory, Constants.Paths.Files);

        try
        {
            if (!Directory.Exists(pathFilesDir))
                Directory.CreateDirectory(pathFilesDir);

            string filePath = Path.Combine(pathFilesDir, $"{Guid.NewGuid()}.jpg");

            TGFile file = await _botClient.GetFile(fileId);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await _botClient.DownloadFile(file, fileStream);
            }

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading or saving file {fileId}. Error: {ex.Message}");
            return null;
        }
    }
}