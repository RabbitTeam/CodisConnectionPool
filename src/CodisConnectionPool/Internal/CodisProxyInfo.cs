using Newtonsoft.Json;

namespace CodisConnectionPool.Internal
{
    internal class CodisProxyInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("proxy_addr")]
        public string ProxyAddress { get; set; }

        [JsonProperty("online")]
        public bool Online { get; set; }

        [JsonProperty("closed")]
        public bool Closed { get; set; }
    }
}