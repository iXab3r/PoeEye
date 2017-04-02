using System.Threading.Tasks;
using RestEase;

namespace PoeEye.StashRealtimeApi.API
{
    public interface IPoeRatesApi
    {
        [Get("getLastChangeId.php")]
        Task<Response<PoeRatesLastChangeIdResponse>> GetLastChangeId();
    }
}