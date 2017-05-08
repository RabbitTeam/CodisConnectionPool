using CodisConnectionPool.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodisConnectionPool
{
    public class CodisConnectionPool : IDisposable
    {
        #region Field

        private readonly CodisZookeeperInfo _codisZookeeperInfo;
        private readonly ILogger<CodisConnectionPool> _logger;
        private readonly Action<ConfigurationOptions> _optionsConfiguration;
        private PollingSelector _pollingSelector;
        private readonly IZookeeperClient _zookeeperClient;

        #endregion Field

        #region Constructor

        public CodisConnectionPool(IOptions<CodisPoolOptions> codisPoolOptions, ILogger<CodisConnectionPool> logger)
        {
            var options = codisPoolOptions.Value;
            if (string.IsNullOrWhiteSpace(options.ZookeeperInfo.Servers))
                throw new ArgumentException("没有提供任何Zookeeper服务器地址！");
            if (string.IsNullOrWhiteSpace(options.ZookeeperInfo.ProxyPath))
                throw new ArgumentException("Codis的Zookeeper代理路径不能为空！");

            _codisZookeeperInfo = options.ZookeeperInfo;
            _logger = logger;
            _optionsConfiguration = options.OptionsConfiguration;
            _zookeeperClient = new ZookeeperClient(options.ZookeeperInfo.Servers);

            Task.Run(async () =>
            {
                //第一次初始化连接
                await InitCodisConnection();

                //如果节点变更则更新连接信息
                await _zookeeperClient.SubscribeChildrenChange(options.ZookeeperInfo.ProxyPath, async (ct, args) =>
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"路径：{args.Path} 发生了变更，类型：{args.Type}，最新的节点信息为：{(args.CurrentChildrens == null ? "null" : string.Join(",", args.CurrentChildrens))}");

                    await InitCodisConnection();
                });
            }).Wait();
        }

        #endregion Constructor

        #region Public Method

        public ConnectionMultiplexer GetConnectionMultiplexer()
        {
            return _pollingSelector.GetConnectionMultiplexer().Value;
        }

        #endregion Public Method

        #region Private Method

        private async Task InitCodisConnection()
        {
            var list = new List<Lazy<ConnectionMultiplexer>>();
            foreach (var children in await _zookeeperClient.GetChildrenAsync(_codisZookeeperInfo.ProxyPath))
            {
                var path = _codisZookeeperInfo.ProxyPath + "/" + children;
                var data = await _zookeeperClient.GetDataAsync(path);
                var content = Encoding.UTF8.GetString(data.ToArray());

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug(content);

                var info = JsonConvert.DeserializeObject<CodisProxyInfo>(content);

                list.Add(new Lazy<ConnectionMultiplexer>(() =>
                {
                    var options = ConfigurationOptions.Parse(info.ProxyAddress + ",proxy=Twemproxy");
                    _optionsConfiguration?.Invoke(options);
                    return ConnectionMultiplexer.Connect(options);
                }));
            }

            _pollingSelector = new PollingSelector(list.ToArray());
        }

        #endregion Private Method

        #region IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            try
            {
                _zookeeperClient?.Dispose();
            }
            catch
            {
                // ignored
            }
            _pollingSelector?.Dispose();
        }

        #endregion IDisposable
    }
}