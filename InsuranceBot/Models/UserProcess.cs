using InsuranceBot.Enums;

namespace InsuranceBot.Models;

public class UserProcess
{
    public StepProcess Step { get; set; }
    public InsuranceDetails InsuranceDetails { get; set; }

    public UserProcess()
    {
        Step = StepProcess.Start;
        InsuranceDetails = new InsuranceDetails();
    }
}