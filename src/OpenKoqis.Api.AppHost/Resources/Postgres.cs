namespace OpenKoqis.Api.AppHost.Resources;

public static class Postgres
{
    /// <summary>
    /// Injects a PostgreSQL resource for development.
    /// <para>
    /// Purpose of the service: General Relational DB for OpenKoqis. Each Module consumes his own schema
    /// Modules requires PostgreSQL: Humans
    /// </para>
    /// </summary>
    /// <param name="resourceBuilder">The resource where connection string will be injected.</param>
    /// <param name="appBuilder">Application builder to create new Aspire resources.</param>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <returns>The <see cref="IResourceBuilder{T}"/> with PostgreSQL in chain.</returns>
    public static IResourceBuilder<TDestination> WithPostgres<TDestination>(
        this IResourceBuilder<TDestination> resourceBuilder,
        IDistributedApplicationBuilder appBuilder)
        where TDestination : IResourceWithEnvironment
    {
        var postgresUsername = appBuilder.AddParameter("postgres-username");
        var postgresPassword = appBuilder.AddParameter("postgres-password", secret: true);

        var postgres = appBuilder
            .AddPostgres("openkoqis-postgres", userName: postgresUsername, password: postgresPassword)
            .AddDatabase("openkoqis");

        return resourceBuilder.WithReference(postgres);
    }
}
