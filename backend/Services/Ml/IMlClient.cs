using System.Text.Json;

namespace backend.Services.Ml;

public interface IMlClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
    Task<JsonDocument> PredictAsync(JsonElement requestBody, CancellationToken cancellationToken);
}
