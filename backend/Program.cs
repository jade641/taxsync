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
var databaseProvider = (builder.Configuration["Database:Provider"] ?? "MySql").Trim();
var inMemoryDatabaseName = (builder.Configuration["Database:InMemoryName"] ?? "TaxSyncDevelopment").Trim();
var useInMemoryDatabase = databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase);
var useMySqlDatabase = databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase);

if (!useInMemoryDatabase && !useMySqlDatabase)
{
    throw new InvalidOperationException("Database:Provider must be either 'MySql' or 'InMemory'.");
}

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (useMySqlDatabase && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required when Database:Provider is MySql.");
}

ValidateRuntimeMode(runtimeMode, databaseProvider, connectionString);

var mysqlVersion = new Version(8, 0, 0);
if (useMySqlDatabase)
{
    var databaseServerVersion = builder.Configuration["Database:ServerVersion"] ?? "8.0.0";
    if (!Version.TryParse(databaseServerVersion, out mysqlVersion))
    {
        mysqlVersion = new Version(8, 0, 0);
    }
}

var dbStartupRetries = Math.Max(1, builder.Configuration.GetValue("Database:StartupRetries", 30));
var dbStartupRetryDelaySeconds = Math.Max(1, builder.Configuration.GetValue("Database:StartupRetryDelaySeconds", 2));
var dbConnectionProbeTimeoutSeconds = Math.Max(1, builder.Configuration.GetValue("Database:ConnectionProbeTimeoutSeconds", 3));
var defaultCreateDatabaseOnStartup = !runtimeMode.Equals("Production", StringComparison.OrdinalIgnoreCase)
    && (!useMySqlDatabase || IsLoopbackHost(GetDatabaseHost(connectionString)));
var createDatabaseOnStartup = builder.Configuration.GetValue<bool?>("Database:CreateDatabaseOnStartup")
    ?? defaultCreateDatabaseOnStartup;
var applyMigrationsOnStartup = builder.Configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup")
    ?? false;
var ensureCreatedOnStartup = builder.Configuration.GetValue<bool?>("Database:EnsureCreatedOnStartup")
    ?? true;
var failFastOnDatabaseStartup = builder.Configuration.GetValue<bool?>("Database:FailFastOnStartup")
    ?? false;

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
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

if (useInMemoryDatabase)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase(inMemoryDatabaseName));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString!, new MySqlServerVersion(mysqlVersion), mysqlOptions =>
        {
            mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
        }));
}

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

LogStartupConfiguration(
    app.Services,
    runtimeMode,
    databaseProvider,
    connectionString,
    inMemoryDatabaseName);

var databaseReadyOnStartup = await EnsureDatabaseAsync(
    app.Services,
    databaseProvider,
    connectionString,
    runtimeMode,
    createDatabaseOnStartup,
    applyMigrationsOnStartup,
    ensureCreatedOnStartup,
    failFastOnDatabaseStartup,
    dbConnectionProbeTimeoutSeconds,
    dbStartupRetries,
    TimeSpan.FromSeconds(dbStartupRetryDelaySeconds));

if (databaseReadyOnStartup)
{
    await AdminBootstrapper.EnsureAdminAsync(app.Services, app.Configuration);
}
else
{
    app.Services.GetService<ILoggerFactory>()?
        .CreateLogger("AdminBootstrapper")
        .LogWarning("Skipping admin bootstrap because the database was not reachable during startup.");
}

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
    GetHealthAsync(dbContext, runtimeMode, databaseProvider, connectionString, dbConnectionProbeTimeoutSeconds, cancellationToken)).AllowAnonymous();

app.MapGet("/api/health", (AppDbContext dbContext, CancellationToken cancellationToken) =>
    GetHealthAsync(dbContext, runtimeMode, databaseProvider, connectionString, dbConnectionProbeTimeoutSeconds, cancellationToken)).AllowAnonymous();

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");

static async Task<IResult> GetHealthAsync(
    AppDbContext dbContext,
    string runtimeMode,
    string databaseProvider,
    string? connectionString,
    int connectionProbeTimeoutSeconds,
    CancellationToken cancellationToken)
{
    if (dbContext.Database.IsInMemory())
    {
        return Results.Ok(new
        {
            status = "healthy",
            database = "in-memory",
            mode = runtimeMode,
            timestamp = DateTime.UtcNow,
            service = "Property Taxation & Compliance API"
        });
    }

    if (!databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Json(new
        {
            status = "degraded",
            database = "unsupported-provider",
            mode = runtimeMode,
            timestamp = DateTime.UtcNow,
            service = "Property Taxation & Compliance API"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var databaseReadiness = await TryCheckMySqlReadinessAsync(connectionString, connectionProbeTimeoutSeconds, cancellationToken);

    if (!databaseReadiness.IsReady)
    {
        return Results.Json(new
        {
            status = "degraded",
            database = "unavailable",
            detail = databaseReadiness.Error,
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

static async Task<bool> EnsureDatabaseAsync(
    IServiceProvider services,
    string databaseProvider,
    string? connectionString,
    string runtimeMode,
    bool createDatabaseOnStartup,
    bool applyMigrationsOnStartup,
    bool ensureCreatedOnStartup,
    bool failFastOnStartup,
    int connectionProbeTimeoutSeconds,
    int maxRetries,
    TimeSpan retryDelay)
{
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DatabaseBootstrapper");

    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        logger?.LogInformation(
            "Initialized in-memory database for runtime mode {RuntimeMode}.",
            runtimeMode);
        return true;
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

    if (!createDatabaseOnStartup && !applyMigrationsOnStartup && !ensureCreatedOnStartup && !failFastOnStartup)
    {
        var readiness = await TryCheckMySqlReadinessAsync(connectionString, connectionProbeTimeoutSeconds);

        if (readiness.IsReady)
        {
            logger?.LogInformation("Verified MySQL connectivity and schema readiness for runtime mode {RuntimeMode}.", runtimeMode);
            return true;
        }

        logger?.LogWarning(
            "MySQL was not reachable or ready during startup: {Error}. Continuing application startup; /api/health will report degraded until the database is reachable.",
            readiness.Error ?? "unknown error");
        return false;
    }

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await EnsureDatabaseCoreAsync(
                services,
                connectionString,
                runtimeMode,
                createDatabaseOnStartup,
                applyMigrationsOnStartup,
                ensureCreatedOnStartup,
                logger);
            return true;
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

            if (failFastOnStartup)
            {
                throw;
            }

            logger?.LogWarning(
                "Continuing application startup with database unavailable. /api/health will report degraded until MySQL is reachable.");
            return false;
        }
    }

    return false;
}

static async Task EnsureDatabaseCoreAsync(
    IServiceProvider services,
    string connectionString,
    string runtimeMode,
    bool createDatabaseOnStartup,
    bool applyMigrationsOnStartup,
    bool ensureCreatedOnStartup,
    ILogger? logger)
{
    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);
    var databaseName = connectionBuilder.Database;

    if (createDatabaseOnStartup && !string.IsNullOrWhiteSpace(databaseName))
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
    else if (!string.IsNullOrWhiteSpace(databaseName))
    {
        logger?.LogInformation(
            "Skipping CREATE DATABASE bootstrap for runtime mode {RuntimeMode}. Target database: {Database}.",
            runtimeMode,
            databaseName);
    }

    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (applyMigrationsOnStartup)
    {
        await dbContext.Database.MigrateAsync();
        logger?.LogInformation("Database migrations applied: {Database}", databaseName);
        return;
    }

    if (ensureCreatedOnStartup)
    {
        await dbContext.Database.EnsureCreatedAsync();
        logger?.LogInformation("Database ensured: {Database}", databaseName);
        return;
    }

    var canConnect = await dbContext.Database.CanConnectAsync();
    if (!canConnect)
    {
        throw new InvalidOperationException($"Unable to connect to database '{databaseName}'.");
    }

    await dbContext.Users.AsNoTracking().Take(1).AnyAsync();

    logger?.LogInformation(
        "Verified database connectivity and schema without bootstrap for runtime mode {RuntimeMode}. Database: {Database}.",
        runtimeMode,
        databaseName);
}

static bool IsDatabaseStartupException(Exception exception)
{
    return exception is MySqlException or TimeoutException or OperationCanceledException
        || (exception is InvalidOperationException invalidOperationException
            && invalidOperationException.Message.StartsWith("Unable to connect to database", StringComparison.Ordinal))
        || (exception.InnerException is not null && IsDatabaseStartupException(exception.InnerException));
}

static async Task<(bool IsReady, string? Error)> TryCheckMySqlReadinessAsync(
    string? connectionString,
    int timeoutSeconds,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return (false, "ConnectionStrings:DefaultConnection is not configured.");
    }

    try
    {
        var connectionBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = (uint)timeoutSeconds
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await using var connection = new MySqlConnection(connectionBuilder.ConnectionString);
        await connection.OpenAsync(timeoutCts.Token);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = timeoutSeconds;
        command.CommandText = "SELECT 1 FROM `users` LIMIT 1;";
        await command.ExecuteScalarAsync(timeoutCts.Token);

        return (true, null);
    }
    catch (Exception ex) when (IsDatabaseStartupException(ex))
    {
        return (false, ex.GetBaseException().Message);
    }
}

static void LogStartupConfiguration(
    IServiceProvider services,
    string runtimeMode,
    string databaseProvider,
    string? connectionString,
    string inMemoryDatabaseName)
{
    var logger = services.GetService<ILoggerFactory>()?.CreateLogger("StartupConfiguration");
    if (logger is null)
    {
        return;
    }

    if (databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation(
            "Starting TaxSync backend in {RuntimeMode} mode using {DatabaseProvider} database {Database}.",
            runtimeMode,
            databaseProvider,
            inMemoryDatabaseName);
        return;
    }

    ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);

    logger.LogInformation(
        "Starting TaxSync backend in {RuntimeMode} mode using {DatabaseProvider}. Database target: {Server}:{Port}/{Database} (User={User}).",
        runtimeMode,
        databaseProvider,
        connectionBuilder.Server,
        connectionBuilder.Port,
        connectionBuilder.Database,
        connectionBuilder.UserID);
}

static void ValidateRuntimeMode(string runtimeMode, string databaseProvider, string? connectionString)
{
    if (string.IsNullOrWhiteSpace(runtimeMode))
    {
        throw new InvalidOperationException("TaxSync:RuntimeMode must be set to Local, Docker, or Production.");
    }

    var supportedRuntimeMode = runtimeMode.Equals("Local", StringComparison.OrdinalIgnoreCase)
        || runtimeMode.Equals("Docker", StringComparison.OrdinalIgnoreCase)
        || runtimeMode.Equals("Production", StringComparison.OrdinalIgnoreCase);

    if (!supportedRuntimeMode)
    {
        throw new InvalidOperationException("TaxSync:RuntimeMode must be Local, Docker, or Production.");
    }

    var useInMemoryDatabase = databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase);
    var useMySqlDatabase = databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase);

    if (!useInMemoryDatabase && !useMySqlDatabase)
    {
        throw new InvalidOperationException("Database:Provider must be either MySql or InMemory.");
    }

    if (useInMemoryDatabase)
    {
        if (!runtimeMode.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Database:Provider=InMemory is supported only in Local runtime mode.");
        }

        return;
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required when Database:Provider is MySql.");
    }

    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);
    var databaseHost = connectionBuilder.Server ?? string.Empty;

    if (runtimeMode.Equals("Production", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(databaseHost))
        {
            throw new InvalidOperationException(
                "Production mode requires a non-empty database host in ConnectionStrings:DefaultConnection.");
        }

        return;
    }

    if (runtimeMode.Equals("Docker", StringComparison.OrdinalIgnoreCase) && IsLoopbackHost(databaseHost))
    {
        throw new InvalidOperationException(
            "Docker mode cannot use a loopback database host. Use the Docker service name, for example Server=mysql;Port=3306;...");
    }

}

static bool IsLoopbackHost(string host)
{
    return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
}

static string GetDatabaseHost(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return string.Empty;
    }

    var connectionBuilder = new MySqlConnectionStringBuilder(connectionString);

    return connectionBuilder.Server ?? string.Empty;
}

static string EscapeMySqlIdentifier(string identifier)
{
    return identifier.Replace("`", "``");
}