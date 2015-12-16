namespace ProxyProvider.ProxiesSource.FoxTools
{
    using System.Threading.Tasks;

    using RestEase;

    internal interface IFoxToolsApi
    {
        [Get("Proxy")]
        Task<GetProxiesRootObject> GetProxiesList(
            [Query("type")] FoxProxyType type,
            [Query("limit")] int limit, 
            [Query("country")] string countryISOCode,
            [Query("uptime")] double maxUptimeInSeconds);
    }
}