namespace InsuranceBot.Enums;

// Steps of insurance registration
public enum StepProcess
{
    Start,
    IdCardUpload,
    VRDFrontSideUpload,
    VRDBackSideUpload,
    ConfirmData,
    CostCalculation,
    IssuanceInsurancePolicy,
    Finish,
    None
}
