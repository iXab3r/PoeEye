using System;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Squirrel.Core;
using PoeShared.Squirrel.Scaffolding;
using PoeShared.UI;

namespace PoeShared.Squirrel.Updater;

internal sealed class BasicAuthFileDownloader : IFileDownloader
{
    private static readonly IFluentLog Log = typeof(BasicAuthFileDownloader).PrepareLogger();

    private readonly NetworkCredential credentials;

    public BasicAuthFileDownloader(NetworkCredential credentials)
    {
        this.credentials = credentials;
    }

    public async Task DownloadFile(string url, string targetFile, Action<int> progress)
    {
        using var wc = CreateClient();
        using var progressAnchors = Observable.FromEventPattern<DownloadProgressChangedEventHandler, DownloadProgressChangedEventArgs>(
                h => wc.DownloadProgressChanged += h,
                h => wc.DownloadProgressChanged -= h)
            .Where(x => progress != null)
            .Sample(UiConstants.UiThrottlingDelay)
            .SubscribeSafe(x => progress(x.EventArgs.ProgressPercentage), Log.HandleUiException);
        var outputDirectory = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        try
        {
            Log.Debug($"[WebClient.DownloadFile] Downloading file to '{targetFile}', uri: {url} ");
            await wc.DownloadFileTaskAsync(url, targetFile);
            progress(100);
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to download {url} to {targetFile}", e);
            progress(0);
            throw;
        }
    }

    public async Task<byte[]> DownloadUrl(string url)
    {
        using var wc = CreateClient();
        Log.Debug($"[WebClient.DownloadUrl] Downloading data, uri: {url} ");

        return await wc.DownloadDataTaskAsync(url);
    }

    public Task<long> GetSize(string url)
    {
        if (!string.IsNullOrEmpty(credentials.UserName))
        {
            throw new NotSupportedException("Operation is not supported for BasicAuth client with credentials");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new FormatException($"Failed to parse URI: {uri}");
        }

        return Utility.GetRemoteFileSize(uri);
    }

    private WebClient CreateClient()
    {
        var result = new WebClient
        {
            Credentials = credentials
        };

        if (!string.IsNullOrEmpty(credentials.UserName))
        {
            var credentialsBuilder = new StringBuilder();
            credentialsBuilder.Append(credentials.UserName);
            if (!string.IsNullOrEmpty(credentials.Password))
            {
                credentialsBuilder.Append($":{credentials.Password}");
            }

            var credentialsString = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentialsBuilder.ToString()));
            result.Headers[HttpRequestHeader.Authorization] = $"Basic {credentialsString}";
        }

        return result;
    }
}