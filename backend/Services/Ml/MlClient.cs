using System.Net.Http.Json;
using System.Text.Json;

namespace backend.Services.Ml;

public sealed class MlClient(HttpClient httpClient) : IMlClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<JsonDocument> PredictAsync(JsonElement requestBody, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "predict",
            requestBody,
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"ML service error ({(int)response.StatusCode}): {errorText}");
        }

        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
    }
}
