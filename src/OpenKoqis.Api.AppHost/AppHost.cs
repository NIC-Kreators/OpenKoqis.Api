using OpenKoqis.Api.AppHost.Resources;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api")
    .WithMongo(builder)
    .WithPostgres(builder)
    .WithMqtt(builder)
    .WithKeycloak(builder);

builder.Build().Run();
