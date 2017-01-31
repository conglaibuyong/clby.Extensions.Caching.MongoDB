using Microsoft.Extensions.Internal;
using MongoDB.Driver;
using System;
using Microsoft.Extensions.Caching.Distributed;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace clby.Extensions.Caching.MongoDB
{
    internal class DatabaseOperations : IDatabaseOperations
    {
        private static object lockObj = new object();
        private static IMongoDatabase MongoDatabase = null;
        private static IMongoCollection<CacheEntiry> MongoCollection = null;

        protected string ConnectionString { get; }
        protected string DbName { get; }
        protected string CollectionName { get; }
        protected ISystemClock SystemClock { get; }

        public DatabaseOperations(string connectionString, string dbName, string collectionName, ISystemClock systemClock)
        {
            this.ConnectionString = connectionString;
            this.DbName = dbName;
            this.CollectionName = collectionName;
            this.SystemClock = systemClock;

            var Client = new MongoClient(connectionString);
            MongoDatabase = Client.GetDatabase(dbName);
            MongoCollection = MongoDatabase.GetCollection<CacheEntiry>(collectionName);
        }


        public void DeleteCacheItem(string key)
        {
            var filter = Builders<CacheEntiry>.Filter;
            MongoCollection.DeleteOne(filter.Eq(t => t.Id, key));
        }

        public async Task DeleteCacheItemAsync(string key)
        {
            var filter = Builders<CacheEntiry>.Filter;
            await MongoCollection.DeleteOneAsync(filter.Eq(t => t.Id, key));
        }

        public void DeleteExpiredCacheItems()
        {
            var filter = Builders<CacheEntiry>.Filter;
            MongoCollection.DeleteMany(filter.Lt(t => t.ExpiresAtTime, SystemClock.UtcNow.LocalDateTime));
        }


        public byte[] GetCacheItem(string key)
        {
            return GetCacheItem(key, true);
        }

        public async Task<byte[]> GetCacheItemAsync(string key)
        {
            return await GetCacheItemAsync(key, true);
        }

        public void RefreshCacheItem(string key)
        {
            GetCacheItem(key, false);
        }

        public async Task RefreshCacheItemAsync(string key)
        {
            await GetCacheItemAsync(key, false);
        }

        protected byte[] GetCacheItem(string key, bool includeValue)
        {
            var filter = Builders<CacheEntiry>.Filter;
            var query = filter.And(filter.Eq(t => t.Id, key), filter.Gte(t => t.ExpiresAtTime, SystemClock.UtcNow.LocalDateTime));
            var bd = MongoCollection.Find<CacheEntiry>(query);
            if (!includeValue)
            {
                bd = bd.Project<CacheEntiry>(Builders<CacheEntiry>.Projection.Exclude(t => t.Value));
            }
            var ce = bd.FirstOrDefault();
            if (ce == null || ce.Value == BsonNull.Value) return null;

            var update = Builders<CacheEntiry>.Update;
            var ExpiresAtTime = DateTime.Now.Subtract(ce.AbsoluteExpiration) <= ce.SlidingExpirationInSeconds
                ? ce.AbsoluteExpiration
                : DateTime.Now.Add(ce.SlidingExpirationInSeconds);
            MongoCollection.UpdateOne(query, update.Set(t => t.ExpiresAtTime, ExpiresAtTime));

            return includeValue ? ce.Value.AsByteArray : null;
        }
        protected async Task<byte[]> GetCacheItemAsync(string key, bool includeValue)
        {
            var filter = Builders<CacheEntiry>.Filter;
            var query = filter.And(filter.Eq(t => t.Id, key), filter.Gte(t => t.ExpiresAtTime, SystemClock.UtcNow.LocalDateTime));
            var bd = MongoCollection.Find<CacheEntiry>(query);
            if (!includeValue)
            {
                bd = bd.Project<CacheEntiry>(Builders<CacheEntiry>.Projection.Exclude(t => t.Value));
            }
            var ce = bd.FirstOrDefault();
            if (ce == null || ce.Value == BsonNull.Value) return null;

            var update = Builders<CacheEntiry>.Update;
            var ExpiresAtTime = DateTime.Now.Subtract(ce.AbsoluteExpiration) <= ce.SlidingExpirationInSeconds
                ? ce.AbsoluteExpiration
                : DateTime.Now.Add(ce.SlidingExpirationInSeconds);
            await MongoCollection.UpdateOneAsync(query, update.Set(t => t.ExpiresAtTime, ExpiresAtTime));

            return includeValue ? ce.Value.AsByteArray : null;
        }


        public void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            DateTimeOffset utcNow = SystemClock.UtcNow;
            DateTimeOffset? absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            DateTimeOffset ExpiresAtTime = options.SlidingExpiration.HasValue
                ? utcNow + options.SlidingExpiration.Value
                : absoluteExpiration.Value;

            var filter = Builders<CacheEntiry>.Filter;
            var update = Builders<CacheEntiry>.Update;
            MongoCollection.UpdateOne(
                filter.Eq(t => t.Id, key),
                update
                    .Set(t => t.Value, new BsonBinaryData(value))
                    .Set(t => t.ExpiresAtTime, ExpiresAtTime.LocalDateTime)
                    .Set(t => t.AbsoluteExpiration, absoluteExpiration.HasValue ? absoluteExpiration.Value.LocalDateTime : DateTime.MinValue)
                    .Set(t => t.SlidingExpirationInSeconds, options.SlidingExpiration.Value),
                new UpdateOptions() { IsUpsert = true });
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            DateTimeOffset utcNow = SystemClock.UtcNow;
            DateTimeOffset? absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            DateTimeOffset ExpiresAtTime = options.SlidingExpiration.HasValue
                ? utcNow + options.SlidingExpiration.Value
                : absoluteExpiration.Value;

            var filter = Builders<CacheEntiry>.Filter;
            var update = Builders<CacheEntiry>.Update;
            await MongoCollection.UpdateOneAsync(
                filter.Eq(t => t.Id, key),
                update
                    .Set(t => t.Value, new BsonBinaryData(value))
                    .Set(t => t.ExpiresAtTime, ExpiresAtTime.LocalDateTime)
                    .Set(t => t.AbsoluteExpiration, absoluteExpiration.HasValue ? absoluteExpiration.Value.LocalDateTime : DateTime.MinValue)
                    .Set(t => t.SlidingExpirationInSeconds, options.SlidingExpiration.Value),
                new UpdateOptions() { IsUpsert = true });
        }

        protected DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            DateTimeOffset? result = default(DateTimeOffset?);
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                result = new DateTimeOffset?(utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value));
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= utcNow)
                {
                    throw new InvalidOperationException("The absolute expiration value must be in the future.");
                }
                result = new DateTimeOffset?(options.AbsoluteExpiration.Value);
            }
            return result;
        }

        protected void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs to be provided.");
            }
        }

    }
}
