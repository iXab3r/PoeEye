namespace ProxyProvider.ProxiesSource.FoxTools
{
    using Newtonsoft.Json;

    internal class FoxProxy
    {
        [JsonProperty("ip")]
        public string Address { get; set; }

        public int Port { get; set; }

        public float Uptime { get; set; }

        public FoxProxyYesNoAny Available { get; set; }

        public FoxProxyType Type { get; set; }

        public FoxProxyCountry Country { get; set; }
    }
}