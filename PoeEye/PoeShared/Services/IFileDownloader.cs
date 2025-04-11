namespace PoeShared.Services;

public interface IFileDownloader 
{
    Task DownloadFile(string url, string targetFile, Action<int> progress);

    Task<byte[]> DownloadUrl(string url);
        
    Task<long> GetSize(string url);
}