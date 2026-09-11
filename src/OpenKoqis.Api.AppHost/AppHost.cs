var builder = DistributedApplication.CreateBuilder(args);

// Main Database
var mongoUsername = builder.AddParameter("mongo-username");
var mongoPassword = builder.AddParameter("mongo-password", secret: true);

var mongo = builder
    .AddMongoDB("openkoqis-mongo", userName: mongoUsername, password: mongoPassword)
    // Solved error: "MongoDB cannot start: Linux kernel versions 6.19 and newer has a known incompatibility with this version of MongoDB."
    // Remove when mongodb official image will migrate to the linux 7.1+ base image.
    .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var mongoDb = mongo.AddDatabase("openkoqis");

// Message Bus with IoT devices (mqtt via eclipse-mosquitto)
var mosquittoConfigsRoot = $"{builder.Environment.ContentRootPath}/config/mosquitto";

var messageBus = builder
    .AddContainer("openkoqis-mqtt", "eclipse-mosquitto")
    .WithEndpoint(name: "mqtt", port: 1883, targetPort: 1883)
    .WithBindMount($"{mosquittoConfigsRoot}/config", "/mosquitto/config")
    .WithBindMount($"{mosquittoConfigsRoot}/logs", "/mosquitto/logs")
    .WithBindMount($"{mosquittoConfigsRoot}/data", "/mosquitto/data")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithReference(mongoDb)
    .WithReference(messageBus.GetEndpoint("mqtt"));

builder.Build().Run();
