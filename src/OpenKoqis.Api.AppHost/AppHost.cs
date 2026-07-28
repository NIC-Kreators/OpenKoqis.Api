var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.OpenKoqis_Api>("openkoqis-api");

builder.Build().Run();
