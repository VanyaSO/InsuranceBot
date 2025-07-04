using InsuranceBot.Models.DTO;
using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Parsing.Generated;
using Mindee.Product.Generated;

namespace InsuranceBot.Services;

public class MindeeService
{
    private readonly ILogger<MindeeService> _logger;

    public MindeeService(ILogger<MindeeService> logger)
    {
        _logger = logger;
    }

    public async Task<IdCardDto?> GetIdCardDataAsync(string filePath)
    {
        return await GetDocumentDataAsync<IdCardDto>("id_card",
            filePath,
            fields => new IdCardDto
            {
                FullName = $"{GetFieldValue(fields, "full_name")} {GetFieldValue(fields, "patronymic")}",
                DateOfBirth = GetFieldValue(fields, "date_of_birth"),
                DocumentNumber = GetFieldValue(fields, "document_no")
            });
    }

    public async Task<VrdDto?> GetVrdDataAsync(string filePath)
    {
        return await GetDocumentDataAsync<VrdDto>("vehicle_registration_certificate__front_side",
            filePath,
            fields => new VrdDto
            {
                CarNumber = GetFieldValue(fields, "vehicle_registration_number")
            });
    }

    private async Task<T?> GetDocumentDataAsync<T>(
        string endpoint,
        string filePath,
        Func<Dictionary<string, GeneratedFeature>, T> mapper) where T : class
    {
        var fields = await GetDocumentFieldsAsync(endpoint, filePath);
        return fields != null ? mapper(fields) : null;
    }

    private async Task<Dictionary<string, GeneratedFeature>?> GetDocumentFieldsAsync(string endpointName,
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File does not exist: {filePath}");
            return null;
        }

        string? apiKey = Environment.GetEnvironmentVariable("MINDEE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Mindee API Key is missing");
            return null;
        }

        MindeeClient mindeeClient = new MindeeClient(apiKey);
        var inputSource = new LocalInputSource(filePath);

        CustomEndpoint endpoint = new CustomEndpoint(
            endpointName,
            accountName: "VanyaSOmil",
            version: "1"
        );

        try
        {
            var response = await mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);
            return response.Document?.Inference.Prediction.Fields;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error while getting document data. Endpoint: {endpointName}, File: {filePath}");
            return null;
        }
    }

    private string GetFieldValue(Dictionary<string, GeneratedFeature> fields, string key) =>
        fields[key].AsStringField().Value;
}