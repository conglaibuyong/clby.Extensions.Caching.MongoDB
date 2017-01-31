using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;

namespace clby.Extensions.Caching.MongoDB
{
    internal interface IDatabaseOperations
    {
        byte[] GetCacheItem(string key);

        Task<byte[]> GetCacheItemAsync(string key);

        void RefreshCacheItem(string key);

        Task RefreshCacheItemAsync(string key);

        void DeleteCacheItem(string key);

        Task DeleteCacheItemAsync(string key);

        void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options);

        Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options);

        void DeleteExpiredCacheItems();
    }
}
