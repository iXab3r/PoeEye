using System;
using System.Threading.Tasks;

namespace PoeShared.Squirrel.Core
{
    public interface IFileDownloader 
    {
        Task DownloadFile(string url, string targetFile, Action<int> progress);

        Task<byte[]> DownloadUrl(string url);
        
        Task<long> GetSize(string url);
    }
}