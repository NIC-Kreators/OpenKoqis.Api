using OpenKoqis.Api.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var mongoDb = Mongo.GetMongo(builder);
var postgres = Postgres.GetPostgres(builder);
var messageBusEndpoint = MessageBus.GetMessageBus(builder);
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithReference(mongoDb)
    .WithReference(postgres)
    .WithReference(messageBusEndpoint)
    .WithReference(keycloak);

builder.Build().Run();
