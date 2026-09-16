using OpenKoqis.Api.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// General Mongo
var mongoDb = Mongo.GetMongo(builder);

// PostgreSQL
var postgres = Postgres.GetPostgres(builder);

// Message Bus with IoT devices (mqtt via eclipse-mosquitto)
var messageBusEndpoint = MessageBus.GetMessageBus(builder);

// Keycloak
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithReference(mongoDb)
    .WithReference(postgres)
    .WithReference(messageBusEndpoint)
    .WithReference(keycloak);

builder.Build().Run();
