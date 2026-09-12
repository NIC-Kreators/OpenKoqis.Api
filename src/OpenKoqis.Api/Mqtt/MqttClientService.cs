using System.Buffers;
using System.Text;
using System.Text.Json;
using Mediator;
using MQTTnet;
using OpenKoqis.Application.Features.Bins.Commands;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Mqtt;

public class MqttClientService(
    IConfiguration config,
    ILogger<MqttClientService> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();
    private readonly MqttClientOptions _options = BuildOptions(config);
    private readonly MqttClientSubscribeOptions _subscribeOptions = BuildSubscribeOptions(config);

    private static MqttClientOptions BuildOptions(IConfiguration config)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(config.GetValue<string>("MQTT_HOST"), config.GetValue<int>("MQTT_PORT"))
            .WithClientId(config.GetValue<string>("MQTT_CLIENT_ID"));

        if (!config.GetValue<bool>("MQTT_ALLOW_ANONYMOUS"))
        {
            builder.WithCredentials(config.GetValue<string>("MQTT_USERNAME"), config.GetValue<string>("MQTT_PASSWORD"));
        }

        return builder.Build();
    }

    private static MqttClientSubscribeOptions BuildSubscribeOptions(IConfiguration config)
    {
        var filterBuilder = new MqttClientSubscribeOptionsBuilder();
        var topics = config.GetSection("Mqtt:Topics").Get<string[]>() ?? [];

        foreach (var topic in topics)
        {
            filterBuilder.WithTopicFilter(topic);
        }

        return filterBuilder.Build();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var binId = e.ApplicationMessage.Topic.Split('/')[1];
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
            var telemetry = JsonSerializer.Deserialize<BinTelemetry>(payload);

            if (telemetry is null)
            {
                logger.LogWarning("Invalid telemetry data for bin {Id}: {Payload}", binId, payload);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await mediator.Send(new UpdateBinTelemetryCommand(binId, telemetry), ct);

            result.Switch(
                _ => logger.LogInformation("Updated bin {Id} via MQTT", binId),
                errors => logger.LogWarning("Failed to update telemetry for bin {Id}. Errors: {Errors}",
                    binId, string.Join(", ", errors.Select(err => err.Description)))
            );
        };

        await _client.ConnectAsync(_options, ct);
        await _client.SubscribeAsync(_subscribeOptions, ct);
        logger.LogInformation("Subscribed to MQTT topics and connected successfully");

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);
        }

        await _client.DisconnectAsync(cancellationToken: ct);
        logger.LogInformation("Disconnected from MQTT server");
    }
}
