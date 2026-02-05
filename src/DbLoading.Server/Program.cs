using System.Text.Json;
using DbLoading.Application.Catalog;
using DbLoading.Application.Db2;
using DbLoading.Application.Export;
using DbLoading.Application.Runs;
using DbLoading.Database;
using DbLoading.Database.Mock;
using DbLoading.Infrastructure.Auth;
using DbLoading.Infrastructure.Catalog;
using DbLoading.Infrastructure.Config;
using DbLoading.Infrastructure.Db2;
using DbLoading.Infrastructure.Export;
using DbLoading.Infrastructure.Runs;
using DbLoading.Server.Hubs;
using DbLoading.Server.Middleware;
using DbLoading.Server.Runs;
using NLog;
using NLog.Web;
using DbLoading.Auth;
using AppAuth = DbLoading.Application.Auth;
using AuthLib = DbLoading.Auth;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddOpenApi();

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

    builder.Services.AddMockDatabase();

    Func<string, Dictionary<string, string>?, string> userIdGenerator = (login, claims) =>
    {
        var databaseId = claims?.GetValueOrDefault("databaseId") ?? "";
        var managerId = claims?.GetValueOrDefault("managerId") ?? "";
        var streamId = claims?.GetValueOrDefault("streamId") ?? "";
        return $"{login}@{databaseId}#{managerId}@{streamId}";
    };

    builder.Services.AddDbLoadingAuth(
        builder.Configuration,
        userIdGenerator,
        events =>
        {
            events.OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            };
        });

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
    builder.Services.AddSingleton<IDb2SessionFactory>(sp =>
        new DbConnectionFactoryAdapter(sp.GetRequiredService<IDbConnectionFactory>()));

    builder.Services.AddSignalR();

    builder.Services.AddScoped<ICatalogService, CatalogService>();
    builder.Services.AddSingleton<AppAuth.ITokenService>(sp =>
        new TokenService(sp.GetRequiredService<AuthLib.ITokenService>()));
    builder.Services.AddSingleton<AppAuth.ISessionService>(sp =>
        new SessionService(sp.GetRequiredService<AuthLib.ISessionService>()));
    builder.Services.AddSingleton<IRunService, RunService>();
    builder.Services.AddSingleton<IRunEventBroadcaster, RunEventBroadcaster>();

    var app = builder.Build();

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
