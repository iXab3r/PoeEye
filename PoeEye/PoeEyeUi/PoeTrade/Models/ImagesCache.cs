namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Http;

    using Prism;

    internal sealed class ImagesCache
    {
        private readonly IFactory<IHttpClient> httpClientFactory;
        private readonly IScheduler bgScheduler;
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);

        private readonly string CacheFolderName = "Cache";

        private readonly IDictionary<string, IObservable<FileInfo>> loadingImages = new ConcurrentDictionary<string, IObservable<FileInfo>>();

        public ImagesCache(
                [NotNull] IFactory<IHttpClient> httpClientFactory,
                [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => httpClientFactory);
            Guard.ArgumentNotNull(() => bgScheduler);
            
            this.httpClientFactory = httpClientFactory;
            this.bgScheduler = bgScheduler;
        }

        public IObservable<FileInfo> ResolveImageByUri(Uri imageUri)
        {
            Log.Instance.Trace($"[ItemsCache.ResolveImageByUri] Resolving image '{imageUri}'...");
            var outputFilePath = ConstructPath(imageUri.AbsolutePath);

            IObservable<FileInfo> inProgress;
            if (loadingImages.TryGetValue(outputFilePath, out inProgress))
            {
                Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image is currently loading, returning source");
                return inProgress;
            }

            if (File.Exists(outputFilePath))
            {
                Log.Instance.Trace($"[ItemsCache.ResolveImageByUri] Image is already loaded, cache path '{outputFilePath}'");
                return Observable.Return(new FileInfo(outputFilePath));
            }

            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image '{imageUri}' is not loaded, downloading it...");
            var httpClient = httpClientFactory.Create();
            var result = httpClient
                .GetStreamAsync(imageUri)
                .ObserveOn(bgScheduler)
                .Select(x => LoadImageFromStream(outputFilePath, x))
                .Publish();

            loadingImages[outputFilePath] = result;

            result.Connect();

            return result;
        }

        private FileInfo LoadImageFromStream(string outputFilePath, Stream dataStream)
        {
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Starting downloading image to cache...\r\n\tFilePath: '{outputFilePath}'");

#if DEBUG
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Atrificial delay");
            Thread.Sleep(ArtificialDelay);
#endif

            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(outputFilePath))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (var outputStream = File.OpenWrite(outputFilePath))
            {
                dataStream.CopyTo(outputStream);
            }
            loadingImages.Remove(outputFilePath);

            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image was saved to file '{outputFilePath}'");
            return new FileInfo(outputFilePath);
        }

        private string ConstructPath(string imageUri)
        {
            var cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CacheFolderName);
            var uriHash = Sha256(imageUri);

            var extension = Path.HasExtension(imageUri) ? Path.GetExtension(imageUri) : string.Empty;

            var fileName = uriHash;
            if (!string.IsNullOrWhiteSpace(extension))
            {
                fileName = Path.ChangeExtension(fileName, extension);
            }

            return Path.Combine(cachePath, fileName);
        }

        private static string Sha256(string valueToHash)
        {
            using (var crypt = new SHA256Managed())
            {
                var hash = string.Empty;
                var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(valueToHash), 0, Encoding.ASCII.GetByteCount(valueToHash));
                return crypto.Aggregate(hash, (current, theByte) => current + theByte.ToString("x2"));
            }
        }
    }
}