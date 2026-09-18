using OpenKoqis.Api.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak");

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithMongo(builder)
    .WithPostgres(builder)
    .WithMqtt(builder)
    .WithReference(keycloak);

builder.Build().Run();
