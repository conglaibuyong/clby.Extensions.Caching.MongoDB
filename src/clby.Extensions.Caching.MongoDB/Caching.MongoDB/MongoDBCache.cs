using clby.Extensions.Misc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace clby.Extensions.Caching.MongoDB
{
    public class MongoDBCache : IDistributedCache
    {
        private readonly IDatabaseOperations _dbOperations;

        private readonly ISystemClock _systemClock;

        private readonly TimeSpan _defaultSlidingExpiration;


        public MongoDBCache(IOptions<MongoDBCacheOptions> options)
        {
            MongoDBCacheOptions value = options.Value;

            Ensure.IsNotNull(value.ConnectionString, "ConnectionString");
            Ensure.IsNotNull(value.DbName, "DbName");
            Ensure.IsNotNull(value.CollectionName, "CollectionName");
            Ensure.IsGreaterThanOrEqualTo(value.DefaultSlidingExpiration, TimeSpan.Zero, "DefaultSlidingExpiration");

            _systemClock = (value.SystemClock ?? new SystemClock());
            _defaultSlidingExpiration = value.DefaultSlidingExpiration;
            _dbOperations = new DatabaseOperations(value.ConnectionString, value.DbName, value.CollectionName, _systemClock);
        }


        public byte[] Get(string key)
        {
            Ensure.IsNotNull(key, "key");

            byte[] arg = _dbOperations.GetCacheItem(key);
            return arg;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            Ensure.IsNotNull(key, "key");

            byte[] arg = await _dbOperations.GetCacheItemAsync(key);
            return arg;
        }

        public void Refresh(string key)
        {
            Ensure.IsNotNull(key, "key");

            _dbOperations.RefreshCacheItem(key);
        }

        public async Task RefreshAsync(string key)
        {
            Ensure.IsNotNull(key, "key");

            await _dbOperations.RefreshCacheItemAsync(key);
        }

        public void Remove(string key)
        {
            Ensure.IsNotNull(key, "key");

            _dbOperations.DeleteCacheItem(key);
        }

        public async Task RemoveAsync(string key)
        {
            Ensure.IsNotNull(key, "key");

            await _dbOperations.DeleteCacheItemAsync(key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Ensure.IsNotNull(key, "key");
            Ensure.IsNotNull(value, "value");
            Ensure.IsNotNull(options, "options");

            this.GetOptions(ref options);
            _dbOperations.SetCacheItem(key, value, options);
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Ensure.IsNotNull(key, "key");
            Ensure.IsNotNull(value, "value");
            Ensure.IsNotNull(options, "options");

            this.GetOptions(ref options);
            await _dbOperations.SetCacheItemAsync(key, value, options);
        }


        private void GetOptions(ref DistributedCacheEntryOptions options)
        {
            if (!options.AbsoluteExpiration.HasValue
                && !options.AbsoluteExpirationRelativeToNow.HasValue
                && !options.SlidingExpiration.HasValue)
            {
                DistributedCacheEntryOptions expr = new DistributedCacheEntryOptions();
                expr.SetSlidingExpiration(_defaultSlidingExpiration);
                options = expr;
            }
        }

    }
}
