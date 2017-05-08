using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace CodisConnectionPool.Test
{
    public class CodisConnectionPoolTest : IDisposable
    {
        private readonly CodisConnectionPool _codisConnectionPool;

        public CodisConnectionPoolTest()
        {
            var services = new ServiceCollection()
                .AddLogging()
                .AddCodisConnectionPool(options =>
                {
                    options.ZookeeperInfo = new CodisZookeeperInfo
                    {
                        ProxyPath = "/codis3/codis-andreader/proxy",
                        Servers = "192.168.1.184:2181,192.168.1.185:2181,192.168.1.186:2181"
                    };
                }).BuildServiceProvider();

            _codisConnectionPool = services.GetRequiredService<CodisConnectionPool>();
        }

        [Fact]
        public void HighAvailabilityTest()
        {
            var connection = _codisConnectionPool.GetConnectionMultiplexer();
            var configuration = connection.Configuration;

            connection = _codisConnectionPool.GetConnectionMultiplexer();
            Assert.NotEqual(configuration, connection.Configuration);
        }

        [Fact]
        public void BasicTest()
        {
            var connection = _codisConnectionPool.GetConnectionMultiplexer();
            var database = connection.GetDatabase();

            var key = Guid.NewGuid().ToString();
            database.StringSet(key, "123456", TimeSpan.FromSeconds(5));

            Assert.True(database.KeyExists(key));
            Assert.Equal("123456", database.StringGet(key));

            database.KeyDelete(key);

            Assert.False(database.KeyExists(key));
        }

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _codisConnectionPool?.Dispose();
        }

        #endregion IDisposable
    }
}