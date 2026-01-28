using System.Text;
using System.Text.Json;
using DbLoading.Application.Auth;
using DbLoading.Application.Catalog;
using DbLoading.Application.Db2;
using DbLoading.Application.Export;
using DbLoading.Application.Runs;
using DbLoading.Infrastructure.Auth;
using DbLoading.Infrastructure.Catalog;
using DbLoading.Infrastructure.Config;
using DbLoading.Infrastructure.Db2;
using DbLoading.Infrastructure.Export;
using DbLoading.Infrastructure.Runs;
using DbLoading.Server.Hubs;
using DbLoading.Server.Middleware;
using DbLoading.Server.Runs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();

// CORS for Angular development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
var key = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configuration
var configPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "config");
var absoluteConfigPath = Path.GetFullPath(configPath);
if (!Directory.Exists(absoluteConfigPath))
{
    throw new DirectoryNotFoundException($"Config directory not found: {absoluteConfigPath}");
}
builder.Services.AddSingleton(new ConfigReader(absoluteConfigPath));

var appConfigPath = Path.Combine(absoluteConfigPath, "app.json");
var appConfig = JsonSerializer.Deserialize<AppConfig>(
    File.ReadAllText(appConfigPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Failed to load app.json");
builder.Services.AddSingleton(appConfig);

try
{
    Directory.CreateDirectory(appConfig.Output.RootPath);
    logger.Info("Output root ensured: {0}", appConfig.Output.RootPath);
}
catch (Exception ex)
{
    logger.Error(ex, "Cannot create output root: {0}", appConfig.Output.RootPath);
    throw new InvalidOperationException(
        $"Output rootPath is invalid or not accessible: '{appConfig.Output.RootPath}'. Fix config/app.json (output.rootPath).",
        ex);
}

builder.Services.AddSingleton<IOutputWriter, FileSlicerWriter>();
builder.Services.AddSingleton<ISqlModifier, SqlModifierService>();
builder.Services.AddSingleton<IDb2SessionFactory, Db2SessionFactoryStub>();

// SignalR
builder.Services.AddSignalR();

// Application services
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<IRunService, RunService>();
builder.Services.AddSingleton<IRunEventBroadcaster, RunEventBroadcaster>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionLoggingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RunsHub>("/hubs/runs");

logger.Info("Application started");

app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
