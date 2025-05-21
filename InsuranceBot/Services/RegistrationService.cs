using System.Collections.Concurrent;
using InsuranceBot.Enums;
using InsuranceBot.Exentions;
using InsuranceBot.Helpers;
using InsuranceBot.Models;

namespace InsuranceBot.Services;

public class RegistrationService
{
    private ConcurrentDictionary<long, UserProcess> _progress;
    private HashSet<StepProcess> _photoUploadSteps;
    private readonly OpenAiService _openAiService;

    public RegistrationService(OpenAiService openAiService)
    {
        _progress = new();
        _photoUploadSteps = new()
        {
            StepProcess.IdCardUpload,
            StepProcess.VrdUpload
        };
        _openAiService = openAiService;
    }

    public void StartRegistration(long userId) => _progress.GetOrAdd(userId, new UserProcess());

    public void RemoveUserProgress(long userId) => _progress.TryRemove(userId, out _);

    public bool IsExpectingUploadPhoto(long userId) => _progress.TryGetValue(userId, out var userProcess) &&
                                                       _photoUploadSteps.Contains(userProcess.Step);

    public StepProcess GetCurrentStep(long userId)
    {
        if (_progress.TryGetValue(userId, out var userProcess))
            return userProcess.Step;

        return StepProcess.None;
    }

    public void MoveToNextStep(long userId)
    {
        _progress.AddOrUpdate(userId, new UserProcess(), (_, userProcess) =>
        {
            userProcess.Step = userProcess.Step switch
            {
                StepProcess.Start => StepProcess.IdCardUpload,
                StepProcess.IdCardUpload => StepProcess.VrdUpload,
                StepProcess.VrdUpload => StepProcess.ConfirmData,
                StepProcess.ConfirmData => StepProcess.CostCalculation,
                StepProcess.CostCalculation => StepProcess.IssuanceInsurancePolicy,
                StepProcess.IssuanceInsurancePolicy => StepProcess.Finish
            };

            return userProcess;
        });
    }

    public void SetStep(long userId, StepProcess step)
    {
        if (_progress.TryGetValue(userId, out var userProcess))
            userProcess.Step = step;
    }

    public async Task<string?> GetStepMessageAsync(long userId)
    {
        var step = GetCurrentStep(userId);
        
        // Dictionary with context for step or default message
        Dictionary<string, string>? context = step switch
        {
            StepProcess.Start => new Dictionary<string, string>
            {
                ["context"] = $"This is the first step in the insurance registration process. Ask them to click the {ButtonProcess.Start.GetDescription()} button.",
                ["default"] = $"Click '{ButtonProcess.Start.GetDescription()}' to begin registration."
            },

            StepProcess.IdCardUpload => new Dictionary<string, string>
            {
                ["context"] = "Say the user to upload an ID card photo.",
                ["default"] = "Upload a photo of your ID card."
            },

            StepProcess.VrdUpload => new Dictionary<string, string>
            {
                ["context"] = "Say good. Say the user to upload a photo of the vehicle registration document (front side).",
                ["default"] = "Upload a photo of the front side of your vehicle document."
            },
            
            StepProcess.ConfirmData => new Dictionary<string, string>
            {
                ["context"] = $"Ask the user to confirm the data: {GetConfirmationMessage(userId)}. Without emoji!!!",
                ["default"] = GetConfirmationMessage(userId)
            },

            StepProcess.CostCalculation => new Dictionary<string, string>
            {
                ["context"] = $"Inform that the insurance cost is {Constants.Insurance.DefaultCost}. Ask if the user confirms. Without emoji!!!",
                ["default"] = "Insurance cost: $100. Do you confirm?"
            },

            StepProcess.IssuanceInsurancePolicy => new Dictionary<string, string>
            {
                ["context"] = "Notify that the insurance policy is being created and ask to wait.",
                ["default"] = "Please wait, the process of creating an insurance policy is in progress."
            },

            StepProcess.Finish => new Dictionary<string, string>
            {
                ["context"] = "Tell them the insurance is ready. Thank the user. Tell them it was a pleasure working with them.",
                ["default"] = "Your car insurance is ready. Thank you for your trust, have a nice day!"
            },

            _ => null
        };


        if (context == null)
            return null;

        string? response = await _openAiService.GenerateMessageAsync(context["context"]);
        return response ?? context.GetValueOrDefault("default", "Error. Please try again");
    }

    public bool IsUserProgressExist(long userId) => _progress.ContainsKey(userId);

    public InsuranceDetails? GetInsuranceDetails(long userId) =>
        _progress.TryGetValue(userId, out var userProcess) ? userProcess.InsuranceDetails : null;

    private string GetConfirmationMessage(long userId)
    {
        InsuranceDetails? details = GetInsuranceDetails(userId);
        if (details != null)
            return
                $"Full Name: {details.FullName}\nDate of Birth: {details.DateOfBirth}\nID Card Number: {details.IdCardNumber}\nCar Number: {details.CarNumber}";

        return string.Empty;
    }
    
    public UserProcess? GetUserProcess(long userId) => _progress.GetValueOrDefault(userId, null);

    public bool IsDiscussing(long userId) =>
        _progress.TryGetValue(userId, out var userProcess) && userProcess.IsDiscussing;
}