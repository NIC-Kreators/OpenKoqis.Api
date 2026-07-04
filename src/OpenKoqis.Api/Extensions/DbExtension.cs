using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenKoqis.Api.Options;
using OpenKoqis.Api.Services;

namespace OpenKoqis.Api.Extensions;

public static class DbExtension
{
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoOptions>(configuration.GetSection("MongoSettings"));

        var connectionString = configuration.GetValue<string>("MONGO_CONNECTION_STRING")
                               ?? throw new InvalidOperationException("MONGO_CONNECTION_STRING is not configured.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddSingleton<MongoDbService>();

        return services;
    }
}
