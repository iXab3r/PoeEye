using System;
using System.Net.Http;
using System.Threading.Tasks;
using RestEase;

namespace PoeEye.StashRealtimeApi.API
{
    public interface IStashApi
    {
        [Get("public-stash-tabs")]
        [Header("Accept-Encoding", "gzip")]
        Task<Response<StashApiResponse>> PublicStashTabs([Query("id")] string nextChangeId);

        [Get("public-stash-tabs")]
        [Header("Accept-Encoding", "gzip")]
        Task<HttpResponseMessage> PublicStashTabsRaw([Query("id")] string nextChangeId);
    }
}