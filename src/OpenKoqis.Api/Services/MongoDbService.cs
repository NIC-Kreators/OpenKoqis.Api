using MongoDB.Driver;
using Microsoft.Extensions.Options;
using OpenKoqis.Api.Options;
using OpenKoqis.Domain.Models;

namespace OpenKoqis.Api.Services;

public class MongoDbService
{
    private const string UsersCollectionName = "Users";
    private const string BinsCollectionName = "Bins";

    public IMongoCollection<User> Users { get; }
    public IMongoCollection<Bin> Bins { get; }

    public MongoDbService(IMongoDatabase database)
    {
        Users = database.GetCollection<User>(UsersCollectionName);
        Bins = database.GetCollection<Bin>(BinsCollectionName);
    }
}
