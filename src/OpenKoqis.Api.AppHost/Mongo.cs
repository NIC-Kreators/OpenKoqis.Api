namespace OpenKoqis.Api.AppHost;

public static class Mongo
{
    public static IResourceBuilder<MongoDBDatabaseResource> GetMongo(IDistributedApplicationBuilder builder)
    {
        var mongoUsername = builder.AddParameter("mongo-username");
        var mongoPassword = builder.AddParameter("mongo-password", secret: true);

        var mongo = builder
            .AddMongoDB("openkoqis-mongo", userName: mongoUsername, password: mongoPassword)
            // Solved error: "MongoDB cannot start: Linux kernel versions 6.19 and newer has a known incompatibility with this version of MongoDB."
            // Remove when mongodb official image will migrate to the linux 7.1+ base image.
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);

        // Split into 1 DB per OpenKoqis Module in the future. If some module will require the mongo. It maybe even deleted in the future.
        return mongo.AddDatabase("openkoqis");
    }
}
