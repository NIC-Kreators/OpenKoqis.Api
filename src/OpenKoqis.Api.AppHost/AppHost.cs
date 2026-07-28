var builder = DistributedApplication.CreateBuilder(args);

// Main Database
var mongoUsername = builder.AddParameter("mongo-username", value: "develop_admin");
var mongoPassword = builder.AddParameter("mongo-password", secret: true);

var mongo = builder
    .AddMongoDB("openkoqis-mongo", userName: mongoUsername, password: mongoPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var mongoDb = mongo.AddDatabase("openkoqis");

// Message Bus with IoT devices (mqtt via eclipse-mosquitto). Maybe it should be a NuGet package or use NATS
var messageBus = builder
    .AddContainer("openkoqis-mqtt", "eclipse-mosquitto")
    .WithEndpoint(name: "mqtt", port: 1883, targetPort: 1883)
    .WithBindMount("../../config/mosquitto/config", "/mosquitto/config")
    .WithBindMount("../../config/mosquitto/logs", "/mosquitto/logs")
    .WithBindMount("../../config/mosquitto/data", "/mosquitto/data")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithReference(mongoDb)
    .WithReference(messageBus.GetEndpoint("mqtt"));

builder.Build().Run();
