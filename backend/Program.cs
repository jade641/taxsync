using System.Net.Http.Headers;
using System.Text.Json;
using backend.Services.Ml;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddOptions<MlOptions>()
    .Bind(builder.Configuration.GetSection("MlService"));

builder.Services.AddHttpClient<IMlClient, MlClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MlOptions>>().Value;

    var baseUrl = opts.BaseUrl;
    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapGet("/api/ml/health", async (IMlClient mlClient, CancellationToken ct) =>
    {
        var ok = await mlClient.IsHealthyAsync(ct);
        return ok ? Results.Ok(new { status = "ok" }) : Results.Problem("ML service not reachable");
    })
    .WithName("MlHealth");

app.MapPost("/api/ml/predict", async (JsonElement body, IMlClient mlClient, CancellationToken ct) =>
    {
        using var resultDoc = await mlClient.PredictAsync(body, ct);
        return Results.Json(resultDoc.RootElement.Clone());
    })
    .WithName("MlPredict");

app.Run();

internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}