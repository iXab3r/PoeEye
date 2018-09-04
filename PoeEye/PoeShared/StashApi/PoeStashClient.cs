using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using CsQuery;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using PoeShared.StashApi.ProcurementLegacy;
using RestSharp;

namespace PoeShared.StashApi
{
    internal sealed class PoeStashClient : IPoeStashClient
    {
        private const string CharacterApiPortal = @"https://www.pathofexile.com";

        private readonly IPoeLeagueApiClient leagueClient;
        private readonly NetworkCredential credentials;
        private readonly IGearTypeAnalyzer gearTypeAnalyzer;
        private readonly bool useSessionId;

        private readonly IRestClient client;

        public string SessionId { get; private set; }

        public bool IsAuthenticated { get; private set; } = false;

        public string AccountName { get; private set; }

        public string Email => credentials.UserName;

        public PoeStashClient(
            [NotNull] IPoeLeagueApiClient leagueClient,
            [NotNull] NetworkCredential credentials,
            [NotNull] IGearTypeAnalyzer gearTypeAnalyzer,
            bool useSessionId = false)
        {
            Guard.ArgumentNotNull(leagueClient, nameof(leagueClient));
            Guard.ArgumentNotNull(credentials, nameof(credentials));
            Guard.ArgumentNotNullOrEmpty(() => credentials.UserName);
            Guard.ArgumentNotNullOrEmpty(() => credentials.Password);
            Guard.ArgumentNotNull(gearTypeAnalyzer, nameof(gearTypeAnalyzer));

            this.leagueClient = leagueClient;
            this.credentials = credentials;
            this.gearTypeAnalyzer = gearTypeAnalyzer;
            this.useSessionId = useSessionId;

            if (useSessionId)
            {
                SessionId = credentials.Password;
            }

            client = new RestClient(CharacterApiPortal)
            {
                CookieContainer = new CookieContainer(),
                FollowRedirects = false,
            };
        }

        public void Authenticate()
        {
            AuthenticateAsync().Wait();
        }
        
        public async Task AuthenticateAsync()
        {
            if (IsAuthenticated)
            {
                throw new InvalidOperationException($"Already authenticated with email '{credentials.UserName}'");
            }

            if (useSessionId)
            {
                AuthenticateThroughSessionId();
            }
            else
            {
                await AuthenticateSimple();
            }
        }

        private void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
            {
                throw new InvalidOperationException($"Not authenticated, email: '{credentials.UserName}'");
            }
        }

        private string ExtractErrors(string rawHtml)
        {
            var query = CQ.Create(rawHtml);

            var text = query["ul[class=errors]"]?.Text();

            return text;
        }


        private string RequestLoginToken()
        {
            var request = new RestRequest("login") { Method = Method.GET };

            var response = client.Execute(request);

            var parser = CQ.CreateDocument(response.Content);
            var hash = parser["input[type=hidden][name=hash]"]?.Attr("value");

            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ApplicationException($"Could not read hash from response (length: {response.Content?.Length})");
            }

            return hash;
        }

        private void RefreshAccountName()
        {
            EnsureAuthenticated();

            var request = new RestRequest("my-account") { Method = Method.GET };

            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Could not get account name(code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            var parser = CQ.CreateDocument(response.Content);

            var accountName = parser["div[class=profile-details] div[class=details-inner] div[class=details] h1[class=name]"]?.Text();

            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ApplicationException($"Could not extract account name from response(code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            AccountName = accountName;
        }

        private async Task AuthenticateSimple()
        {
            var hash = RequestLoginToken();

            var loginRequest = new RestRequest("login") { Method = Method.POST };
            loginRequest.AddParameter("login_email", credentials.UserName);
            loginRequest.AddParameter("login_password", credentials.Password);
            loginRequest.AddParameter("hash", hash);
            loginRequest.AddParameter("remember_me", "0");
            loginRequest.AddParameter("login", "Login");

            var response = await client.ExecuteTaskAsync(loginRequest);

            var poeSessionIdCookie = response.Cookies.SingleOrDefault(x => x.Name == "POESESSID");
            if (string.IsNullOrEmpty(poeSessionIdCookie?.Value))
            {
                var error = ExtractErrors(response.Content);
                throw new AuthenticationException($"Could not retrieve POESESSID (code: {response.StatusCode}, length: {response.ContentLength}, error: '{error}'), email: {credentials.UserName}, password.Length: {credentials.Password.Length}, cookies: {response.Cookies.DumpToTextRaw()}");
            }
            SessionId = poeSessionIdCookie.Value;

            IsAuthenticated = true;
            RefreshAccountName();
        }

        private void AuthenticateThroughSessionId()
        {
            client.CookieContainer.Add(new Cookie("POESESSID", SessionId, "/", ".pathofexile.com"));

            IsAuthenticated = true;
            RefreshAccountName();
        }

        public IStash GetStash(int index, string league)
        {
            Guard.ArgumentIsBetween(() => index, 0, byte.MaxValue, true);
            Guard.ArgumentNotNullOrEmpty(() => league);

            return GetStashAsync(index, league).Result;
        }

        public async Task<IStash> GetStashAsync(int stashIdx, string league)
        {
            Guard.ArgumentIsBetween(() => stashIdx, 0, byte.MaxValue, true);
            Guard.ArgumentNotNullOrEmpty(() => league);

            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-stash-items") { Method = Method.GET };
            request.AddParameter("league", league);
            request.AddParameter("tabIndex", stashIdx);
            request.AddParameter("tabs", 1);
            request.AddParameter("accountName", AccountName);

            var response = await client.ExecuteTaskAsync<Stash>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve stash #{stashIdx} in league {league} (code: {response.StatusCode}, content-length: {response.ContentLength})\nResponse data: {response.Content.DumpToText()}");
            }
            PostProcessStash(response.Data);

            foreach (var item in response.Data?.Items.EmptyIfNull().Where(x => x.TypeLine != null))
            {
                item.ItemType = gearTypeAnalyzer.Resolve(item.TypeLine);
            }

            response.Data.Tabs = response.Data
                .Tabs
                .EmptyIfNull()
                .Where(x => !x.Hidden)
                .ToList();

            return response.Data;
        }

        public ICharacter[] GetCharacters()
        {
            return GetCharactersAsync().Result;
        }
        
        public async Task<ICharacter[]> GetCharactersAsync()
        {
            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-characters") { Method = Method.GET };
            var response = await client.ExecuteTaskAsync<List<Character>>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve characters list(code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            return response.Data.OfType<ICharacter>().ToArray();
        }

        public IInventory GetInventory(string characterName)
        {
            Guard.ArgumentNotNull(characterName, nameof(characterName));

            return GetInventoryAsync(characterName).Result;
        }
        
        public async Task<IInventory> GetInventoryAsync(string characterName)
        {
            Guard.ArgumentNotNullOrEmpty(() => characterName);
            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-items") { Method = Method.GET };
            request.AddParameter("character", characterName);
            request.AddParameter("accountName", AccountName);

            var response = await client.ExecuteTaskAsync<Inventory>(request);
            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve inventory of character {characterName} @ {AccountName} (code: {response.StatusCode}, content-length: {response.ContentLength})");
            }
            PostProcessInventory(response.Data);

            return response.Data;
        }
        
        public Task<ILeague[]> GetLeaguesAsync()
        {
            return leagueClient.GetLeaguesAsync();
        }

        private void PostProcessInventory(Inventory inventory)
        {
            inventory?.Items?.ForEach(x => x.CleanupItemName());
        }

        private void PostProcessStash(Stash stash)
        {
            stash?.Items?.ForEach(x => x.CleanupItemName());
        }
    }
}
