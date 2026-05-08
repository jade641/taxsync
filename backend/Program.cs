using System.Text;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Models;
using backend.Services;
using backend.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var runtimeMode = (builder.Configuration["TaxSync:RuntimeMode"] ?? "Local").Trim();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

ValidateRuntimeMode(runtimeMode, connectionString);

var databaseServerVersion = builder.Configuration["Database:ServerVersion"] ?? "8.0.0";
if (!Version.TryParse(databaseServerVersion, out var mysqlVersion))
{
    mysqlVersion = new Version(8, 0, 0);
}

var dbStartupRetries = Math.Max(1, builder.Configuration.GetValue("Database:StartupRetries", 30));
var dbStartupRetryDelaySeconds = Math.Max(1, builder.Configuration.GetValue("Database:StartupRetryDelaySeconds", 2));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one frontend origin.");
}

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Property Taxation & Compliance API", 
        Version = "v1",
        Description = "API for Property Taxation and Compliance Reporting System - Davao Region"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(mysqlVersion), mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
    }));

builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    options.IterationCount = 100_000;
});

// Identity configuration
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddSignInManager()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();


// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-key-min-32-characters-long-for-security";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaxSync";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TaxSyncUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AdminAccountRepairService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<ITaxService, TaxService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IGeographyService, GeographyService>();

// Email Service
builder.Services.Configure<backend.Services.Email.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<backend.Services.Email.IEmailService, backend.Services.Email.EmailService>();

// Excel Export Service
builder.Services.AddScoped<backend.Services.Export.IExcelExportService, backend.Services.Export.ExcelExportService>();

// RDLC Report Service
builder.Services.AddScoped<backend.Services.Reports.IReportService, backend.Services.Reports.ReportService>();

// ML Service with Polly retry and timeout policies
builder.Services.Configure<backend.Services.Ml.MlOptions>(
    builder.Configuration.GetSection("MlService"));

builder.Services.AddHttpClient<backend.Services.Ml.IMlService, backend.Services.Ml.MlService>((serviceProvider, client) =>
{
    var mlOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<backend.Services.Ml.MlOptions>>().Value;
    client.BaseAddress = new Uri(mlOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(mlOptions.TimeoutSeconds);
})
.AddPolicyHandler(backend.Services.Ml.MlService.GetRetryPolicy())
.AddPolicyHandler(backend.Services.Ml.MlService.GetTimeoutPolicy());

var app = builder.Build();

LogStartupConfiguration(app.Services, runtimeMode, connectionString, allowedOrigins);

await EnsureDatabaseAsync(
    app.Services,
    connectionString,
    dbStartupRetries,
    TimeSpan.FromSeconds(dbStartupRetryDelaySeconds));
await AdminBootstrapper.EnsureAdminAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Property Taxation API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseForwardedHeaders();

if (builder.Configuration.GetValue("Https:Redirect", false))
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", (AppDbContext dbContext, CancellationToken cancellationToken) =>
    GetHealthAsync(dbContext, runtimeMode, cancellationToken)).AllowAnonymous();

app.MapGet("/api/health", (AppDbContext dbContext, CancellationToken cancellationToken) =>
    GetHealthAsync(dbContext, runtimeMode, cancellationToken)).AllowAnonymous();

app.Run();

static async Task<IResult> GetHealthAsync(
    AppDbContext dbContext,
    string runtimeMode,
    CancellationToken cancellationToken)
{
    var databaseReady = await dbContext.Database.CanConnectAsync(cancellationToken);

    if (!databaseReady)
    {
        return Results.Json(new
        {
            status = "degraded",
            database = "unavailable",
            mode = runtimeMode,
            timestamp = DateTime.UtcNow,
            service = "Property Taxation & Compliance API"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "healthy",
        database = "ready",
        mode = runtimeMode,
        timestamp = DateTime.UtcNow,
        service = "Property Taxation & Compliance API"
    });
}

static async Task EnsureDatabaseAsync(
    IServiceProvider services,
    string connectionString,
    int maxRetries,
    TimeSpan retryDelay)
{
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DatabaseBootstrapper");

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await EnsureDatabaseCoreAsync(services, connectionString, logger);
            return;
        }
        catch (Exception ex) when (attempt < maxRetries && IsDatabaseStartupException(ex))
        {
            logger?.LogWarning(
                ex,
                "Database is not ready yet. Retrying startup check {Attempt}/{MaxRetries} in {DelaySeconds} seconds.",
                attempt,
                maxRetries,
                retryDelay.TotalSeconds);

            await Task.Delay(retryDelay);
        }
        catch (Exception ex) when (IsDatabaseStartupException(ex))
        {
            logger?.LogError(
                ex,
                "Database startup failed after {MaxRetries} attempts.",
                maxRetries);
            throw;
        }
    }
}

static async Task EnsureDatabaseCoreAsync(
    IServiceProvider services,
    string connectionString,
    ILogger? logger)
{
    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);
    var databaseName = connectionBuilder.Database;

    if (!string.IsNullOrWhiteSpace(databaseName))
    {
        var serverConnectionBuilder = new MySqlConnectionStringBuilder(connectionBuilder.ConnectionString)
        {
            Database = string.Empty
        };

        logger?.LogInformation(
            "Checking MySQL availability at {Server}:{Port} for database {Database}.",
            serverConnectionBuilder.Server,
            serverConnectionBuilder.Port,
            databaseName);

        await using var connection = new MySqlConnection(serverConnectionBuilder.ConnectionString);
        await connection.OpenAsync();

        logger?.LogInformation(
            "Connected to MySQL server {Server}:{Port}. Ensuring database {Database} exists.",
            serverConnectionBuilder.Server,
            serverConnectionBuilder.Port,
            databaseName);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{EscapeMySqlIdentifier(databaseName)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync();
    }

    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await dbContext.Database.EnsureCreatedAsync();
    logger?.LogInformation("Database ensured: {Database}", databaseName);
}

static bool IsDatabaseStartupException(Exception exception)
{
    return exception is MySqlException or TimeoutException
        || (exception.InnerException is not null && IsDatabaseStartupException(exception.InnerException));
}

static void LogStartupConfiguration(
    IServiceProvider services,
    string runtimeMode,
    string connectionString,
    string[] allowedOrigins)
{
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("StartupConfiguration");
    if (logger is null)
    {
        return;
    }

    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);

    logger.LogInformation(
        "Starting TaxSync backend in {RuntimeMode} mode. Database target: {Server}:{Port}/{Database} (User={User}). Allowed origins: {AllowedOrigins}.",
        runtimeMode,
        connectionBuilder.Server,
        connectionBuilder.Port,
        connectionBuilder.Database,
        connectionBuilder.UserID,
        allowedOrigins.Length == 0 ? "<none>" : string.Join(", ", allowedOrigins));
}

static void ValidateRuntimeMode(string runtimeMode, string connectionString)
{
    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);
    var databaseHost = connectionBuilder.Server;

    if (runtimeMode.Equals("Docker", StringComparison.OrdinalIgnoreCase) && IsLoopbackHost(databaseHost))
    {
        throw new InvalidOperationException(
            "Docker mode cannot use a loopback database host. Use the Docker service name, for example Server=mysql;Port=3306;...");
    }

    if (runtimeMode.Equals("Local", StringComparison.OrdinalIgnoreCase) &&
        databaseHost.Equals("mysql", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Local mode cannot use the Docker-only database host 'mysql'. Use 127.0.0.1 or localhost for local development.");
    }
}

static bool IsLoopbackHost(string host)
{
    return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
}

static string EscapeMySqlIdentifier(string identifier)
{
    return identifier.Replace("`", "``");
}