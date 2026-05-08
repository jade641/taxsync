using backend.DTOs;
using backend.Services.Ml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MachineLearningController : ControllerBase
{
    private readonly IMlService _mlService;
    private readonly ILogger<MachineLearningController> _logger;

    public MachineLearningController(IMlService mlService, ILogger<MachineLearningController> logger)
    {
        _mlService = mlService;
        _logger = logger;
    }

    /// <summary>
    /// Check ML service health status
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            var isHealthy = await _mlService.IsHealthyAsync(HttpContext.RequestAborted);
            
            if (isHealthy)
            {
                return Ok(new
                {
                    status = "healthy",
                    service = "ML Service",
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                service = "ML Service",
                timestamp = DateTime.UtcNow,
                message = "ML service is not responding"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ML service health");
            return StatusCode(503, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Predict property tax using ML model
    /// </summary>
    [HttpPost("predict-tax")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant")]
    public async Task<IActionResult> PredictTax([FromBody] TaxPredictionRequestDto request)
    {
        try
        {
            if (request.Properties == null || !request.Properties.Any())
            {
                return BadRequest(new { message = "At least one property is required" });
            }

            var result = await _mlService.PredictTaxAsync(request, HttpContext.RequestAborted);
            
            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ML service request failed");
            return StatusCode(503, new
            {
                success = false,
                message = "ML service is unavailable",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting tax");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during tax prediction",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Forecast future tax revenue
    /// </summary>
    [HttpPost("forecast-revenue")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant")]
    public async Task<IActionResult> ForecastRevenue([FromBody] RevenueForecastRequestDto request)
    {
        try
        {
            if (request.HistoricalData == null || request.HistoricalData.Count < 3)
            {
                return BadRequest(new { message = "At least 3 historical data points are required" });
            }

            if (request.ForecastPeriods < 1 || request.ForecastPeriods > 24)
            {
                return BadRequest(new { message = "Forecast periods must be between 1 and 24" });
            }

            var result = await _mlService.ForecastRevenueAsync(request, HttpContext.RequestAborted);
            
            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ML service request failed");
            return StatusCode(503, new
            {
                success = false,
                message = "ML service is unavailable",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forecasting revenue");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during revenue forecasting",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detect anomalous transactions
    /// </summary>
    [HttpPost("detect-anomalies")]
    [Authorize(Roles = "Admin,TaxOfficer,Accountant,Auditor")]
    public async Task<IActionResult> DetectAnomalies([FromBody] AnomalyDetectionRequestDto request)
    {
        try
        {
            if (request.Transactions == null || request.Transactions.Count < 10)
            {
                return BadRequest(new { message = "At least 10 transactions are required for anomaly detection" });
            }

            var result = await _mlService.DetectAnomaliesAsync(request, HttpContext.RequestAborted);
            
            return Ok(new
            {
                success = true,
                data = result,
                summary = new
                {
                    total = result.TotalTransactions,
                    anomalies = result.AnomalyCount,
                    percentage = result.AnomalyPercentage,
                    highSeverity = result.Anomalies.Count(a => a.Severity == "high"),
                    mediumSeverity = result.Anomalies.Count(a => a.Severity == "medium"),
                    lowSeverity = result.Anomalies.Count(a => a.Severity == "low")
                }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ML service request failed");
            return StatusCode(503, new
            {
                success = false,
                message = "ML service is unavailable",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred during anomaly detection",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get sample data for testing ML endpoints
    /// </summary>
    [HttpGet("sample-data/{type}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetSampleData(string type)
    {
        return type.ToLower() switch
        {
            "tax-prediction" => Ok(new
            {
                properties = new[]
                {
                    new
                    {
                        land_area = 250.5,
                        assessed_value = 2500000,
                        property_type = "Residential",
                        region_id = 1,
                        building_age = 10
                    },
                    new
                    {
                        land_area = 500.0,
                        assessed_value = 5000000,
                        property_type = "Commercial",
                        region_id = 2,
                        building_age = 5
                    }
                }
            }),
            
            "revenue-forecast" => Ok(new
            {
                historical_data = Enumerable.Range(1, 12).Select(i => new
                {
                    period = i,
                    amount = 5000000 + (i * 50000) + new Random().Next(-100000, 100000)
                }),
                forecast_periods = 6
            }),
            
            "anomaly-detection" => Ok(new
            {
                transactions = Enumerable.Range(1, 20).Select(i => new
                {
                    transaction_id = i,
                    amount = i == 15 ? 500000 : 50000 + new Random().Next(-5000, 5000),
                    date = DateTime.Now.AddDays(-i),
                    type = "Payment"
                })
            }),
            
            _ => NotFound(new { message = "Invalid sample data type" })
        };
    }
}
