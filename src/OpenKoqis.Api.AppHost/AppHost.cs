using OpenKoqis.Api.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var messageBusEndpoint = MessageBus.GetMessageBus(builder);
var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithMongo(builder)
    .WithPostgres(builder)
    .WithReference(messageBusEndpoint)
    .WithReference(keycloak);

builder.Build().Run();
