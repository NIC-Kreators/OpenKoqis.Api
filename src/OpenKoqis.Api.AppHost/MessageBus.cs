namespace OpenKoqis.Api.AppHost;

public static class MessageBus
{
    public static EndpointReference GetMessageBus(IDistributedApplicationBuilder builder)
    {
        // Message Bus with IoT devices (mqtt via eclipse-mosquitto)
        var mosquittoConfigsRoot = $"{builder.Environment.ContentRootPath}/config/mosquitto";

        var messageBus = builder
            .AddContainer("openkoqis-mqtt", "eclipse-mosquitto")
            .WithEndpoint(name: "mqtt", port: 1883, targetPort: 1883)
            .WithBindMount($"{mosquittoConfigsRoot}/config", "/mosquitto/config")
            .WithBindMount($"{mosquittoConfigsRoot}/logs", "/mosquitto/logs")
            .WithBindMount($"{mosquittoConfigsRoot}/data", "/mosquitto/data")
            .WithLifetime(ContainerLifetime.Persistent);

        return messageBus.GetEndpoint("mqtt");
    }
}
