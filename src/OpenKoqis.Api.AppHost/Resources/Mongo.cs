namespace OpenKoqis.Api.AppHost.Resources;

public static class Mongo
{
    /// <summary>
    /// Injects a MongoDB resource for development.
    /// <para>
    /// Purpose of the service: General DB for OpenKoqis.
    /// It was developed before "Modular" separation, so it's not pinned to any module.
    /// May be deleted if no module won't need for it. Left for old non-modular code.
    /// </para>
    /// </summary>
    /// <param name="resourceBuilder">The resource where connection string will be injected.</param>
    /// <param name="appBuilder">Application builder to create new Aspire resources.</param>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <returns>The <see cref="IResourceBuilder{T}"/> with MongoDB in chain.</returns>
    public static IResourceBuilder<TDestination> WithMongo<TDestination>(
        this IResourceBuilder<TDestination> resourceBuilder,
        IDistributedApplicationBuilder appBuilder)
        where TDestination : IResourceWithEnvironment
    {
        var mongoUsername = appBuilder.AddParameter("mongo-username");
        var mongoPassword = appBuilder.AddParameter("mongo-password", secret: true);

        var mongo = appBuilder
            .AddMongoDB("openkoqis-mongo", userName: mongoUsername, password: mongoPassword)
            // Solved error: "MongoDB cannot start: Linux kernel versions 6.19 and newer has a known incompatibility with this version of MongoDB."
            // Remove when mongodb official image will migrate to the linux 7.1+ base image.
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("openkoqis");

        return resourceBuilder.WithReference(mongo);
    }
}
