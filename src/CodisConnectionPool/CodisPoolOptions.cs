using StackExchange.Redis;
using System;

namespace CodisConnectionPool
{
    public class CodisPoolOptions
    {
        public CodisZookeeperInfo ZookeeperInfo { get; set; }
        public Action<ConfigurationOptions> OptionsConfiguration { get; set; }
    }
}