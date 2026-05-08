namespace backend.DTOs;

// ==================== TAX PREDICTION ====================

public record PropertyFeaturesDto(
    decimal LandArea,
    decimal AssessedValue,
    string PropertyType,
    int RegionId,
    int BuildingAge = 0);

public record TaxPredictionRequestDto(
    List<PropertyFeaturesDto> Properties);

public record TaxPredictionDto(
    decimal LandArea,
    decimal AssessedValue,
    string PropertyType,
    decimal PredictedTax,
    decimal TaxRate,
    double Confidence);

public record TaxPredictionResponseDto(
    List<TaxPredictionDto> Predictions,
    string ModelVersion,
    string Timestamp);

// ==================== REVENUE FORECASTING ====================

public record HistoricalDataPointDto(
    int Period,
    decimal Amount);

public record RevenueForecastRequestDto(
    List<HistoricalDataPointDto> HistoricalData,
    int ForecastPeriods);

public record ForecastDto(
    int Period,
    decimal ForecastedAmount,
    string Trend);

public record ConfidenceIntervalDto(
    int Period,
    decimal LowerBound,
    decimal UpperBound);

public record RevenueForecastResponseDto(
    List<ForecastDto> Forecasts,
    List<ConfidenceIntervalDto> ConfidenceIntervals,
    string ModelVersion,
    string Timestamp);

// ==================== ANOMALY DETECTION ====================

public record TransactionDto(
    int TransactionId,
    decimal Amount,
    DateTime Date,
    string Type);

public record AnomalyDetectionRequestDto(
    List<TransactionDto> Transactions);

public record AnomalyDto(
    int TransactionId,
    decimal Amount,
    DateTime Date,
    string Type,
    double AnomalyScore,
    List<string> Reason,
    string Severity);

public record AnomalyDetectionResponseDto(
    List<AnomalyDto> Anomalies,
    int TotalTransactions,
    int AnomalyCount,
    double AnomalyPercentage,
    string Timestamp);

// ==================== ML SERVICE HEALTH ====================

public record MlHealthResponseDto(
    string Status,
    string Service,
    string Version,
    string Timestamp,
    Dictionary<string, bool> ModelsLoaded);
