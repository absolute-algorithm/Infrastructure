using MongoDB.Driver;

namespace AbsoluteAlgorithm.Infrastructure.Database
{
    /// <summary>
    /// Provides MongoDB initialization support. Unlike relational providers, MongoDB does not
    /// require schema creation or migration scripts — collections and indexes are created on demand.
    /// </summary>
    public static class MongoDbInitializer
    {
        /// <summary>
        /// Validates MongoDB connectivity. MongoDB does not require schema initialization.
        /// </summary>
        /// <param name="connectionString">The connection string for MongoDB.</param>
        public static void Initialize(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(new MongoUrl(connectionString).DatabaseName);
            database.RunCommand<MongoDB.Bson.BsonDocument>("{ping:1}");
        }
    }
}