using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;
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
        private const string LeaguesApiPortal = @"http://api.pathofexile.com";

        private readonly NetworkCredential credentials;
        private readonly IGearTypeAnalyzer gearTypeAnalyzer;
        private readonly bool useSessionId;

        private readonly IRestClient client;

        public string SessionId { get; private set; }

        public bool IsAuthenticated { get; private set; } = false;

        public string AccountName { get; private set; }

        public string Email => credentials.UserName;

        public PoeStashClient(
            [NotNull] NetworkCredential credentials,
            [NotNull] IGearTypeAnalyzer gearTypeAnalyzer,
            bool useSessionId = false)
        {
            Guard.ArgumentNotNull(() => credentials);
            Guard.ArgumentNotNullOrEmpty(() => credentials.UserName);
            Guard.ArgumentNotNullOrEmpty(() => credentials.Password);
            Guard.ArgumentNotNull(() => gearTypeAnalyzer);

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
                FollowRedirects = false
            };
        }

        public void Authenticate()
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
                AuthenticateSimple();
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
            var hash = parser["input[type=hidden][name=hash][id=hash]"]?.Attr("value");

            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ApplicationException($"Could not read hash from response (length: {response.Content?.Length}");
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

        private void AuthenticateSimple()
        {
            var hash = RequestLoginToken();

            var loginRequest = new RestRequest("login") { Method = Method.POST };
            loginRequest.AddParameter("login_email", credentials.UserName);
            loginRequest.AddParameter("login_password", credentials.Password);
            loginRequest.AddParameter("hash", hash);
            loginRequest.AddParameter("login", "Login");

            var response = client.Execute(loginRequest);

            //If we didn't get a redirect, your gonna have a bad time.
            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                var error = ExtractErrors(response.Content);
                throw new AuthenticationException($"Could not authenticate(code: {response.StatusCode}, length: {response.ContentLength}, error: '{error}'), email: {credentials.UserName}, password.Length: {credentials.Password.Length}");
            }

            var poeSessionIdCookie = response.Cookies.SingleOrDefault(x => x.Name == "POESESSID");
            if (poeSessionIdCookie == null || string.IsNullOrEmpty(poeSessionIdCookie.Value))
            {
                throw new AuthenticationException($"Could not retrieve POESESSID (code: {response.StatusCode}, length: {response.ContentLength}), email: {credentials.UserName}, password.Length: {credentials.Password.Length}");
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

            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-stash-items") { Method = Method.GET };
            request.AddParameter("league", league);
            request.AddParameter("tabIndex", index);
            request.AddParameter("tabs", 1);
            request.AddParameter("accountName", AccountName);

            var response = client.Execute<Stash>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve stash #{index} in league {league} (code: {response.StatusCode}, content-length: {response.ContentLength})");
            }
            PostProcessStash(response.Data);

            foreach (var item in response.Data?.Items.EmptyIfNull().Where(x => x.TypeLine != null))
            {
                item.ItemType = gearTypeAnalyzer.Resolve(item.TypeLine);
            }

            response.Data.Tabs = response.Data
                .Tabs
                .EmptyIfNull()
                .Where(x => !x.hidden)
                .ToList();

            return response.Data;
        }

        public ILeague[] GetLeagues()
        {
            EnsureAuthenticated();

            var client = new RestClient(LeaguesApiPortal);
            var request = new RestRequest("leagues") { Method = Method.GET };

            var response = client.Execute<List<League>>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not leagues list (code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            return response.Data.OfType<ILeague>().Where(x => !string.IsNullOrEmpty(x.Id)).ToArray();
        }

        public ICharacter[] GetCharacters()
        {
            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-characters") { Method = Method.GET };
            var response = client.Execute<List<Character>>(request);

            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve characters list(code: {response.StatusCode}, content-length: {response.ContentLength})");
            }

            return response.Data.OfType<ICharacter>().ToArray();
        }

        public IInventory GetInventory(string characterName)
        {
            Guard.ArgumentNotNullOrEmpty(() => characterName);
            EnsureAuthenticated();

            var request = new RestRequest("character-window/get-items") { Method = Method.GET };
            request.AddParameter("character", characterName);
            request.AddParameter("accountName", AccountName);

            var response = client.Execute<Inventory>(request);
            if (response.Data == null)
            {
                throw new ApplicationException($"Could not retrieve inventory of character {characterName} @ {AccountName} (code: {response.StatusCode}, content-length: {response.ContentLength})");
            }
            PostProcessInventory(response.Data);

            return response.Data;
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