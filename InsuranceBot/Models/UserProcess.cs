using InsuranceBot.Enums;

namespace InsuranceBot.Models;

public class UserProcess
{
    public StepProcess Step { get; set; }
    public StepProcess PrevStep { get; set; }
    public InsuranceDetails InsuranceDetails { get; set; }
    public bool IsDiscussing { get; set; }

    public UserProcess()
    {
        Step = StepProcess.Start;
        PrevStep = StepProcess.Start;
        InsuranceDetails = new InsuranceDetails();
    }
}