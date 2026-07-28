using DotNetEnv.Configuration;
using OpenKoqis.Api.Extensions;
using OpenKoqis.Api.Mqtt;
using OpenKoqis.Application.Services;
using OpenKoqis.Infrastructure.Services;
using Scalar.AspNetCore;
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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options
            .WithTitle("OpenKoqis API")
            .WithTheme(ScalarTheme.DeepSpace)
            .ShowOperationId()
            .WithDefaultHttpClient(ScalarTarget.Node, ScalarClient.Undici)
            .AddPreferredSecuritySchemes("BearerAuth")
            .AddHttpAuthentication("BearerAuth", auth =>
            {
                auth.Token = "Your jwt token";
            });
    });
}
app.MapControllers();
app.MapGet("/hello", () => "Hello").Stable();
app.MapGet("/health", () => Results.Ok()).Stable();
app.Run();
