using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoeShared.Scaffolding;

namespace PoeShared.Squirrel.Core;

public sealed partial class PoeUpdateManager
{
    public static async Task<PoeUpdateManager> GitHubUpdateManager(
        string repoUrl,
        IFileDownloader urlDownloader,
        string applicationName = null,
        string rootDirectory = null,
        bool prerelease = false,
        string accessToken = null)
    {
        var repoUri = new Uri(repoUrl);
        var userAgent = new ProductInfoHeaderValue("Squirrel", Assembly.GetExecutingAssembly().GetName().Version?.ToString());
        Log.Debug(() => $"Creating GitHub UpdateManager for {repoUri}, userAgent: {userAgent}");

        if (repoUri.Segments.Length != 3)
        {
            throw new ArgumentException("Repo URL must be to the root URL of the repo e.g. https://github.com/myuser/myrepo");
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

        Log.Debug($"GitHub base address: {baseAddress}");
        using var client = new WebClient() { BaseAddress = baseAddress.ToString() };
        client.Headers.Add(HttpRequestHeader.UserAgent, userAgent.ToString());
        Log.Debug($"Downloading data, URI: {releasesApiBuilder}");
        var serializedData = await client.DownloadStringTaskAsync(releasesApiBuilder.ToString());
        var releases = JsonConvert.DeserializeObject<List<Release>>(serializedData);
        Log.Debug($"Deserialized response, length: {serializedData.Length}");

        var latestReleases = releases
            .Where(x => prerelease || !x.Prerelease)
            .OrderByDescending(x => x.PublishedAt)
            .Take(5)
            .ToArray();
        
        var latestRelease = latestReleases.FirstOrDefault();
        Log.Debug($"Received info about following releases:\n\t{latestReleases.DumpToTable()}");

        if (latestRelease == null)
        {
            throw new InvalidStateException($"could not find a valid Release, releases count: {releases.Count}");
        }
                
        var latestReleaseUrl = latestRelease.HtmlUrl.Replace("/tag/", "/download/");
        Log.Debug($"Latest release URL: {latestReleaseUrl}");
        return new PoeUpdateManager(latestReleaseUrl, urlDownloader, applicationName, rootDirectory);
    }

    [DataContract]
    public class Release
    {
        [DataMember(Name = "prerelease")] public bool Prerelease { get; set; }
        [DataMember(Name = "created_at")] public DateTime CreatedAt { get; set; }
        [DataMember(Name = "published_at")] public DateTime PublishedAt { get; set; }
        [DataMember(Name = "html_url")] public string HtmlUrl { get; set; }
        [DataMember(Name = "tag_name")] public string Tag { get; set; }
        [DataMember(Name = "name")] public string Name { get; set; }
        [DataMember(Name = "id")] public string Id { get; set; }
    }
}