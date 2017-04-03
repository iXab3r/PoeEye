using System.Threading.Tasks;
using RestEase;

namespace PoeEye.StashRealtimeApi.API
{
    public interface IPoeNinjaApi
    {
        [Get("GetStats")]
        Task<Response<PoeNinjaGetStatsResponse>> GetStats();
    }
}