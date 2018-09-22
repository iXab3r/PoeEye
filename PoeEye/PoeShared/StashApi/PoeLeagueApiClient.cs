using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Logging;
using PoeShared.StashApi.DataTypes;
using RestSharp;

namespace PoeShared.StashApi
{
    internal sealed class PoeLeagueApiClient : IPoeLeagueApiClient
    {
        private const string LeaguesApiPortal = @"http://api.pathofexile.com";
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeLeagueApiClient));

        private readonly IRestClient client;

        public PoeLeagueApiClient()
        {
            client = new RestClient(LeaguesApiPortal)
            {
                CookieContainer = new CookieContainer(),
                FollowRedirects = false
            };
        }

        public async Task<ILeague[]> GetLeaguesAsync()
        {
            var request = new RestRequest("leagues") {Method = Method.GET};
            request.AddParameter("type", "main");

            Log.Debug("Requesting leagues list using official API...");
            var response = await client.ExecuteTaskAsync<List<League>>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not leagues list (code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            return response.Data.OfType<ILeague>().Where(x => !string.IsNullOrEmpty(x.Id)).ToArray();
        }
    }
}