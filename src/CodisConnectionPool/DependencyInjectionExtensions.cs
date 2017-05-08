using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodisConnectionPool
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCodisConnectionPool(this IServiceCollection services, Action<CodisPoolOptions> configureOptions)
        {
            services
                .AddOptions()
                .Configure(configureOptions)
                .AddSingleton<CodisConnectionPool, CodisConnectionPool>();
            return services;
        }
    }
}