using clby.Extensions.Caching.MongoDB;
using clby.Extensions.Misc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace clby.Extensions.DependencyInjection
{
    public static class MongoDBCachingServicesExtensions
    {
        public static IServiceCollection AddDistributedMongoDBCache(this IServiceCollection services, Action<MongoDBCacheOptions> setupAction)
        {
            Ensure.IsNotNull(services, "services");
            Ensure.IsNotNull(setupAction, "setupAction");

            OptionsServiceCollectionExtensions.AddOptions(services);
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, MongoDBCache>());
            OptionsServiceCollectionExtensions.Configure<MongoDBCacheOptions>(services, setupAction);
            return services;
        }
    }
}
