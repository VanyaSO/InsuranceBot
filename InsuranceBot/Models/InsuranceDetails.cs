namespace InsuranceBot.Models;

public class InsuranceDetails
{
    public string PolicyNumber { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime EndDate { get; set; }
    public string FullName { get; set; }
    public string DateOfBirth { get; set; }
    public string IdCardNumber { get; set; }
    public string CarNumber { get; set; }
    
    public override string ToString()
    {
        return $"Policy Number: {PolicyNumber}\nIssue Date: {IssueDate:yyyy-MM-dd}\nEnd Date: {EndDate:yyyy-MM-dd}\nFull Name: {FullName}\nDate of Birth: {DateOfBirth}\nID/Passport: {IdCardNumber}\nCar Number: {CarNumber}\n";
    }
}