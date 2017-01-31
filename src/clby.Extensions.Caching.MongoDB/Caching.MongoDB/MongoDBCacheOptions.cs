using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;

namespace clby.Extensions.Caching.MongoDB
{
    public class MongoDBCacheOptions : IOptions<MongoDBCacheOptions>
    {
        public MongoDBCacheOptions()
            : base()
        {
            this.DefaultSlidingExpiration = TimeSpan.FromMinutes(20.0);
        }

        public ISystemClock SystemClock { get; set; }

        public string ConnectionString { get; set; }

        public string DbName { get; set; }

        public string CollectionName { get; set; }

        public TimeSpan DefaultSlidingExpiration { get; set; }

        MongoDBCacheOptions IOptions<MongoDBCacheOptions>.Value
        {
            get
            {
                return this;
            }
        }
    }
}
