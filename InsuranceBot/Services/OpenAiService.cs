using System.Text;
using InsuranceBot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InsuranceBot.Services;

public class OpenAiService
{
    private readonly ILogger<OpenAiService> _logger;
    private readonly string _apiKey;

    private readonly string _apiUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public OpenAiService(ILogger<OpenAiService> logger)
    {
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public async Task<string?> GenerateMessageAsync(string context)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("Gemini API Key is missing");
            return null;
        }

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text =
                                    $"Generate a response to the Telegram bot in English based on the following context: {context}." +
                                    "Use a friendly and professional tone. Include 0-2 relevant emoji." +
                                    "Avoid using greetings like \"hello\" or \"h\" unless they are written in context." +
                                    "Don't say \"thank you\" unless the context clearly requires it. Return only the response text, no text in the request, no Markdown or code." +
                                    "If the context starts with \"Discussing:\", it means the user asked you a question directly and you should contact them as an assistant." +
                                    "A little about you who you should be, you are an assistant who helps to issue car insurance with documents."
                            }
                        }
                    }
                }
            };

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"));

            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseText)["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response.");
            return null;
        }
    }

    public async Task<string?> GenerateInsuranceAsync(InsuranceDetails insuranceDetails)
    {
        if (string.IsNullOrEmpty(_apiKey))
            _logger.LogError("Gemini API Key is missing");

        insuranceDetails.PolicyNumber = new Random().Next(10000, 99999).ToString();
        insuranceDetails.IssueDate = DateTime.UtcNow;
        insuranceDetails.EndDate = DateTime.UtcNow.AddDays(1);

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = GetRequestText(insuranceDetails) }
                        }
                    }
                }
            };

            using HttpClient httpClient = new HttpClient();
            var response = await httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"));

            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseText);

            return json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating insurance");
            return null;
        }
    }

    private string GetRequestText(InsuranceDetails insuranceDetails)
    {
        string template =
            $"Generate a car insurance policy by populating this template with the provided data. Maintain all formatting and field names exactly as in the template. Return only the completed policy without additional commentary.\n\n" +
            "CAR INSURANCE POLICY\n\n" +
            "\ud83d\udd39 Policy Number: {POLICY_NUMBER}\n" +
            "\ud83d\udd39 Issue Date: {ISSUE_DATE}\n" +
            "\ud83d\udd39 Valid Until: {END_DATE}\n\n" +
            "\ud83d\udccc POLICYHOLDER:\n" +
            "\u25b8 Full Name: {FULL_NAME}\n" +
            "\u25b8 Date of Birth: {BIRTH_DATE}\n" +
            "\u25b8 ID/Passport: {ID_NUMBER}\n\n" +
            "\ud83d\ude97 VEHICLE DETAILS:\n" +
            "\u25b8 License Plate: {CAR_NUMBER}\n" +
            "\ud83d\udccb COVERED RISKS:\n" +
            "\u2713 Accident Damage\n" +
            "\u2713 Theft\n" +
            "\u2713 Third-Party Liability\n" +
            "\u2713 Natural Disasters\n" +
            "\u2713 Vandalism\n" +
            "\u2713 Fire\n\n" +
            $"{insuranceDetails.ToString()}";

        return $"{template}";
    }
}