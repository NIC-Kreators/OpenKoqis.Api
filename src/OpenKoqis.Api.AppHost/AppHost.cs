using OpenKoqis.Api.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithMongo(builder)
    .WithPostgres(builder)
    .WithMqtt(builder)
    .WithKeycloak(builder);

builder.Build().Run();
