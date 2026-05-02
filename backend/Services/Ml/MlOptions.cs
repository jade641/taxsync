namespace backend.Services.Ml;

public sealed class MlOptions
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:8000";
    public int TimeoutSeconds { get; init; } = 10;
}
