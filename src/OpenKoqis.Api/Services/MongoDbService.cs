using MongoDB.Driver;
using Microsoft.Extensions.Options;
using OpenKoqis.Api.Options;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Services;

public class MongoDbService
{
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<Bin> Bins { get; }

    public MongoDbService(IMongoDatabase database, IOptions<MongoOptions> options)
    {
        Users = database.GetCollection<User>(options.Value.UsersCollection);
        Bins = database.GetCollection<Bin>(options.Value.BinsCollection);
    }
}
