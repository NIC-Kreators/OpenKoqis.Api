namespace OpenKoqis.Api.AppHost;

public static class Mqtt
{
    /// <summary>
    /// Injects a MQTT resource for development via eclipse-mosquitto.
    /// <para>
    /// Purpose of the service: Message Bus for IoT devices
    /// </para>
    /// </summary>
    /// <param name="resourceBuilder">The resource where connection string will be injected.</param>
    /// <param name="appBuilder">Application builder to create new Aspire resources.</param>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <returns>The <see cref="IResourceBuilder{T}"/> with MQTT in chain.</returns>
    public static IResourceBuilder<TDestination> WithMqtt<TDestination>(
        this IResourceBuilder<TDestination> resourceBuilder, IDistributedApplicationBuilder appBuilder)
        where TDestination : IResourceWithEnvironment
    {
        var mosquittoConfigsRoot = $"{appBuilder.Environment.ContentRootPath}/config/mosquitto";

        var mqttEndpoint = appBuilder
            .AddContainer("openkoqis-mqtt", "eclipse-mosquitto")
            .WithEndpoint(name: "mqtt", port: 1883, targetPort: 1883)
            .WithBindMount($"{mosquittoConfigsRoot}/config", "/mosquitto/config")
            .WithBindMount($"{mosquittoConfigsRoot}/logs", "/mosquitto/logs")
            .WithBindMount($"{mosquittoConfigsRoot}/data", "/mosquitto/data")
            .WithLifetime(ContainerLifetime.Persistent)
            .GetEndpoint("mqtt");

        return resourceBuilder.WithReference(mqttEndpoint);
    }
}
