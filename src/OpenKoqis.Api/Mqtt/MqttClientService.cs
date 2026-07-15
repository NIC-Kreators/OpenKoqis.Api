using System.Buffers;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using OpenKoqis.Application.Features.Bins.Commands;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Mqtt;

public class MqttClientService : BackgroundService
{
    private readonly ILogger<MqttClientService> _logger;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly MqttClientSubscribeOptions _subscribeOptions;

    public MqttClientService(
        IConfiguration config,
        ILogger<MqttClientService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(config.GetValue<string>("MQTT_HOST"), config.GetValue<int>("MQTT_PORT"))
            .WithClientId(config.GetValue<string>("MQTT_CLIENT_ID"));
        _logger.LogDebug("Options for MQTT server is defined");

        var isMqttAllowedAnonymous = config.GetValue<bool>("MQTT_ALLOW_ANONYMOUS");
        if (!isMqttAllowedAnonymous)
        {
            _logger.LogInformation("MQTT is not allowed anonymous connection. Configure username and password");
            optionsBuilder = optionsBuilder.WithCredentials(config.GetValue<string>("MQTT_USERNAME"), config.GetValue<string>("MQTT_PASSWORD"));
        }

        _options = optionsBuilder.Build();
        _logger.LogInformation("Options for MQTT server was built");

        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            var binId = topic.Split('/')[1];
            var telemetry = JsonSerializer.Deserialize<BinTelemetry>(payload);

            if (telemetry is null)
            {
                _logger.LogWarning("Received invalid telemetry data for bin {Id}: {Payload}", binId, payload);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var command = new UpdateBinTelemetryCommand(binId, telemetry);
            var result = await mediator.Send(command);

            result.Switch(
                _ => _logger.LogInformation("Updated bin {Id} via MQTT", binId),
                errors => _logger.LogWarning("Failed to update telemetry for bin {Id}. Errors: {Errors}",
                    binId, string.Join(", ", errors.Select(err => err.Description)))
            );
        };

        var subscribeOptionsFilter = new MqttClientSubscribeOptionsBuilder();
        var topics = config.GetSection("Mqtt:Topics").Get<string[]>() ?? [];
        _logger.LogInformation("Topics for MQTT server are: {Topics}", string.Join(", ", topics));

        foreach (var topic in topics)
            subscribeOptionsFilter.WithTopicFilter(topic);

        _subscribeOptions = subscribeOptionsFilter.Build();
        _logger.LogInformation("Subscribe Options for MQTT server was built");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _client.ConnectAsync(_options, ct);
        _logger.LogInformation("Connected to MQTT server");

        await _client.SubscribeAsync(_subscribeOptions, ct);
        _logger.LogInformation("Subscribed to MQTT topics");

        while (!ct.IsCancellationRequested)
            await Task.Delay(1000, ct);

        _logger.LogInformation("Disconnected from MQTT server");
        await _client.DisconnectAsync(cancellationToken: ct);
    }
}
