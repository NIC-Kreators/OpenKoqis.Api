using DotNetEnv.Configuration;
using OpenKoqis.Api.Extensions;
using OpenKoqis.Api.Mqtt;
using OpenKoqis.Application.Services;
using OpenKoqis.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.WebHost.CaptureStartupErrors(true);
builder.WebHost.UseSetting("detailedErrors", "true");

builder.Configuration
    .AddEnvironmentVariables()
    .AddDotNetEnv();

builder.Host.UseSerilog((context, configuration) =>
                            configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddAuthorizationSecPolicies();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddScoped<MqttClientService>()
    .AddScoped<IUserService, UserService>()
    .AddScoped<IBinService, BinService>()
    .AddScoped<IJwtService, JwtService>()
    .AddScoped<IPasswordHasher, BCryptPasswordHasher>()
    .AddScoped<IAlertService, AlertService>();


var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSerilogRequestLogging();

app.AddScalar();
app.MapControllers();

app.Run();
