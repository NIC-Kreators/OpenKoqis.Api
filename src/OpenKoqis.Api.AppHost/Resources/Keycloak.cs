namespace OpenKoqis.Api.AppHost.Resources;

public static class Keycloak
{
    /// <summary>
    /// Injects a Keycloak resource for development.
    /// <para>
    /// Purpose of the service: Identity Provider for complex Auth scenarios
    /// </para>
    /// </summary>
    /// <param name="resourceBuilder">The resource where connection string will be injected.</param>
    /// <param name="appBuilder">Application builder to create new Aspire resources.</param>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <returns>The <see cref="IResourceBuilder{T}"/> with Keycloak in chain.</returns>
    public static IResourceBuilder<TDestination> WithKeycloak<TDestination>(
        this IResourceBuilder<TDestination> resourceBuilder,
        IDistributedApplicationBuilder appBuilder)
        where TDestination : IResourceWithEnvironment
        => resourceBuilder.WithReference(
            appBuilder.AddKeycloak("openkoqis-keycloak")
        );
}
