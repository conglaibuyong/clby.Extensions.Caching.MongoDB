using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace clby.Extensions.Caching.MongoDB
{
    public class CacheEntiry
    {
        [BsonId]
        public string Id { get; set; }

        public BsonBinaryData Value { get; set; }

        /// <summary>
        /// TTL索引
        /// db.{{CollectionName}}.createIndex({"ExpiresAtTime":1},{expireAfterSeconds:1200})
        /// </summary>
        public DateTime ExpiresAtTime { get; set; }

        public DateTime AbsoluteExpiration { get; set; }

        public TimeSpan SlidingExpirationInSeconds { get; set; }
    }
}
