using System.Net.Http.Json;
using System.Text.Json;
using backend.DTOs;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace backend.Services.Ml;

/// <summary>
/// Enhanced ML service with retry policies and comprehensive error handling
/// </summary>
public interface IMlService
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<TaxPredictionResponseDto> PredictTaxAsync(TaxPredictionRequestDto request, CancellationToken cancellationToken = default);
    Task<RevenueForecastResponseDto> ForecastRevenueAsync(RevenueForecastRequestDto request, CancellationToken cancellationToken = default);
    Task<AnomalyDetectionResponseDto> DetectAnomaliesAsync(AnomalyDetectionRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class MlService : IMlService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MlService> _logger;
    private readonly MlOptions _options;

    public MlService(
        HttpClient httpClient,
        IOptions<MlOptions> options,
        ILogger<MlService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var health = await response.Content.ReadFromJsonAsync<MlHealthResponseDto>(cancellationToken);
                _logger.LogInformation(
                    "ML Service health check: {Status}, Models loaded: {ModelsLoaded}",
                    health?.Status,
                    health?.ModelsLoaded.Count(m => m.Value));
                return true;
            }
            
            _logger.LogWarning("ML Service health check failed with status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML Service health check failed");
            return false;
        }
    }

    public async Task<TaxPredictionResponseDto> PredictTaxAsync(
        TaxPredictionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting tax prediction for {Count} properties", request.Properties.Count);

            var response = await _httpClient.PostAsJsonAsync(
                "predict",
                new
                {
                    properties = request.Properties.Select(p => new
                    {
                        land_area = p.LandArea,
                        assessed_value = p.AssessedValue,
                        property_type = p.PropertyType,
                        region_id = p.RegionId,
                        building_age = p.BuildingAge
                    })
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "ML Service prediction failed: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                throw new HttpRequestException(
                    $"ML Service error ({(int)response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            
            var predictions = jsonResponse.GetProperty("predictions")
                .EnumerateArray()
                .Select(p => new TaxPredictionDto(
                    p.GetProperty("land_area").GetDecimal(),
                    p.GetProperty("assessed_value").GetDecimal(),
                    p.GetProperty("property_type").GetString() ?? "",
                    p.GetProperty("predicted_tax").GetDecimal(),
                    p.GetProperty("tax_rate").GetDecimal(),
                    p.GetProperty("confidence").GetDouble()
                ))
                .ToList();

            var result = new TaxPredictionResponseDto(
                predictions,
                jsonResponse.GetProperty("model_version").GetString() ?? "1.0.0",
                jsonResponse.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString("O")
            );

            _logger.LogInformation("Tax prediction completed successfully for {Count} properties", predictions.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tax prediction");
            throw;
        }
    }

    public async Task<RevenueForecastResponseDto> ForecastRevenueAsync(
        RevenueForecastRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Requesting revenue forecast for {Periods} periods with {DataPoints} historical data points",
                request.ForecastPeriods,
                request.HistoricalData.Count);

            var response = await _httpClient.PostAsJsonAsync(
                "forecast",
                new
                {
                    historical_data = request.HistoricalData.Select(h => new
                    {
                        period = h.Period,
                        amount = h.Amount
                    }),
                    forecast_periods = request.ForecastPeriods
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "ML Service forecast failed: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                throw new HttpRequestException(
                    $"ML Service error ({(int)response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            
            var forecasts = jsonResponse.GetProperty("forecasts")
                .EnumerateArray()
                .Select(f => new ForecastDto(
                    f.GetProperty("period").GetInt32(),
                    f.GetProperty("forecasted_amount").GetDecimal(),
                    f.GetProperty("trend").GetString() ?? "stable"
                ))
                .ToList();

            var confidenceIntervals = jsonResponse.GetProperty("confidence_intervals")
                .EnumerateArray()
                .Select(c => new ConfidenceIntervalDto(
                    c.GetProperty("period").GetInt32(),
                    c.GetProperty("lower_bound").GetDecimal(),
                    c.GetProperty("upper_bound").GetDecimal()
                ))
                .ToList();

            var result = new RevenueForecastResponseDto(
                forecasts,
                confidenceIntervals,
                jsonResponse.GetProperty("model_version").GetString() ?? "1.0.0",
                jsonResponse.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString("O")
            );

            _logger.LogInformation("Revenue forecast completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during revenue forecasting");
            throw;
        }
    }

    public async Task<AnomalyDetectionResponseDto> DetectAnomaliesAsync(
        AnomalyDetectionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Requesting anomaly detection for {Count} transactions", request.Transactions.Count);

            var response = await _httpClient.PostAsJsonAsync(
                "anomaly-detection",
                new
                {
                    transactions = request.Transactions.Select(t => new
                    {
                        transaction_id = t.TransactionId,
                        amount = t.Amount,
                        date = t.Date.ToString("O"),
                        type = t.Type
                    })
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "ML Service anomaly detection failed: {StatusCode} - {Error}",
                    response.StatusCode,
                    errorContent);
                throw new HttpRequestException(
                    $"ML Service error ({(int)response.StatusCode}): {errorContent}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            
            var anomalies = jsonResponse.GetProperty("anomalies")
                .EnumerateArray()
                .Select(a => new AnomalyDto(
                    a.GetProperty("transaction_id").GetInt32(),
                    a.GetProperty("amount").GetDecimal(),
                    DateTime.Parse(a.GetProperty("date").GetString() ?? DateTime.UtcNow.ToString("O")),
                    a.GetProperty("type").GetString() ?? "",
                    a.GetProperty("anomaly_score").GetDouble(),
                    a.GetProperty("reason").EnumerateArray().Select(r => r.GetString() ?? "").ToList(),
                    a.GetProperty("severity").GetString() ?? "low"
                ))
                .ToList();

            var result = new AnomalyDetectionResponseDto(
                anomalies,
                jsonResponse.GetProperty("total_transactions").GetInt32(),
                jsonResponse.GetProperty("anomaly_count").GetInt32(),
                jsonResponse.GetProperty("anomaly_percentage").GetDouble(),
                jsonResponse.GetProperty("timestamp").GetString() ?? DateTime.UtcNow.ToString("O")
            );

            _logger.LogInformation(
                "Anomaly detection completed: {AnomalyCount} anomalies found out of {TotalCount} transactions ({Percentage}%)",
                result.AnomalyCount,
                result.TotalTransactions,
                result.AnomalyPercentage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during anomaly detection");
            throw;
        }
    }

    /// <summary>
    /// Create retry policy for ML service calls
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"ML Service retry {retryCount} after {timespan.TotalSeconds}s");
                });
    }

    /// <summary>
    /// Create timeout policy for ML service calls
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds = 30)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));
    }
}
