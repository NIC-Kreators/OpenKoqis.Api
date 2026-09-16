namespace OpenKoqis.Api.AppHost;

public static class Postgres
{
    public static IResourceBuilder<PostgresDatabaseResource> GetPostgres(IDistributedApplicationBuilder builder)
    {
        var postgresUsername = builder.AddParameter("postgres-username");
        var postgresPassword = builder.AddParameter("postgres-password", secret: true);

        var postgres = builder.AddPostgres(
            "postgres",
            userName: postgresUsername,
            password: postgresPassword);

        return postgres.AddDatabase("openkoqis");
    }
}
