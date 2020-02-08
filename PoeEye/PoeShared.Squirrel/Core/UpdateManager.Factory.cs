﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using PoeShared.Squirrel.Scaffolding;
using Squirrel;

namespace PoeShared.Squirrel.Core
{
    public sealed partial class PoeUpdateManager
    {
        public static async Task<PoeUpdateManager> GitHubUpdateManager(
            string repoUrl,
            string applicationName = null,
            string rootDirectory = null,
            IFileDownloader urlDownloader = null,
            bool prerelease = false,
            string accessToken = null)
        {
            var repoUri = new Uri(repoUrl);
            var userAgent = new ProductInfoHeaderValue("Squirrel", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            if (repoUri.Segments.Length != 3)
            {
                throw new Exception("Repo URL must be to the root URL of the repo e.g. https://github.com/myuser/myrepo");
            }

            var releasesApiBuilder = new StringBuilder("repos")
                .Append(repoUri.AbsolutePath)
                .Append("/releases");

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                releasesApiBuilder.Append("?access_token=").Append(accessToken);
            }

            Uri baseAddress;

            if (repoUri.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
            {
                baseAddress = new Uri("https://api.github.com/");
            }
            else
            {
                // if it's not github.com, it's probably an Enterprise server
                // now the problem with Enterprise is that the API doesn't come prefixed
                // it comes suffixed
                // so the API path of http://internal.github.server.local API location is
                // http://interal.github.server.local/api/v3. 
                baseAddress = new Uri($"{repoUri.Scheme}{Uri.SchemeDelimiter}{repoUri.Host}/api/v3/");
            }

            // above ^^ notice the end slashes for the baseAddress, explained here: http://stackoverflow.com/a/23438417/162694

            using (var client = new HttpClient {BaseAddress = baseAddress})
            {
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
                var response = await client.GetAsync(releasesApiBuilder.ToString());
                response.EnsureSuccessStatusCode();

                var releases = SimpleJson.DeserializeObject<List<Release>>(await response.Content.ReadAsStringAsync());
                var latestRelease = releases
                    .Where(x => prerelease || !x.Prerelease)
                    .OrderByDescending(x => x.PublishedAt)
                    .FirstOrDefault();

                if (latestRelease == null)
                {
                    throw new ApplicationException($"could not find a valid Release, releases count: {releases.Count}");
                }
                
                var latestReleaseUrl = latestRelease.HtmlUrl.Replace("/tag/", "/download/");

                return new PoeUpdateManager(latestReleaseUrl, applicationName, rootDirectory, urlDownloader);
            }
        }

        [DataContract]
        public class Release
        {
            [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }

            [DataMember(Name = "published_at")] public DateTime PublishedAt { get; set; }

            [DataMember(Name = "html_url")] public string HtmlUrl { get; set; }
        }
    }
}