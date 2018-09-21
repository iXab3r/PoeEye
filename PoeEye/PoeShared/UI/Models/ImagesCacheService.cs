using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Communications;
using PoeShared.Prism;
using Unity.Attributes;

namespace PoeShared.UI.Models
{
    internal sealed class ImagesCacheService : IImagesCacheService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImagesCacheService));
        
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);

        private static readonly string CachePath = Environment.ExpandEnvironmentVariables($@"%LOCALAPPDATA%\PoeEye\Cache\");
        private readonly IFactory<IHttpClient> httpClientFactory;

        private readonly IDictionary<string, IObservable<FileInfo>> imagesBeingLoaded = new ConcurrentDictionary<string, IObservable<FileInfo>>();
        private readonly IScheduler uiScheduler;

        public ImagesCacheService(
            [NotNull] IFactory<IHttpClient> httpClientFactory,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            this.httpClientFactory = httpClientFactory;
            this.uiScheduler = uiScheduler;
        }

        public IObservable<FileInfo> ResolveImageByUri(Uri imageUri)
        {
            Log.Trace($"[ItemsCache.ResolveImageByUri] Resolving image '{imageUri}'...");
            var outputFilePath = ConstructPath(imageUri.AbsolutePath);

            IObservable<FileInfo> inProgress;
            if (imagesBeingLoaded.TryGetValue(outputFilePath, out inProgress))
            {
                Log.Debug($"[ItemsCache.ResolveImageByUri] Image is currently loading, returning source");
                return inProgress;
            }

            if (File.Exists(outputFilePath))
            {
                Log.Trace($"[ItemsCache.ResolveImageByUri] Image is already loaded, cache path '{outputFilePath}'");
                return Observable.Return(new FileInfo(outputFilePath)).ObserveOn(uiScheduler);
            }

            Log.Debug($"[ItemsCache.ResolveImageByUri] Image '{imageUri}' is not loaded, downloading it...");
            var httpClient = httpClientFactory.Create();
            var result = httpClient
                         .GetStreamAsync(imageUri)
                         .Select(x => LoadImageFromStream(outputFilePath, x))
                         .Catch(Observable.Empty<FileInfo>())
                         .ObserveOn(uiScheduler)
                         .Publish();

            imagesBeingLoaded[outputFilePath] = result;

            result.Connect();

            return result;
        }

        private FileInfo LoadImageFromStream(string outputFilePath, Stream dataStream)
        {
            Log.Debug($"[ItemsCache.ResolveImageByUri] Starting to download image to cache...\r\n\tFilePath: '{outputFilePath}'");

            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (outputDirectory != null && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (var outputStream = File.OpenWrite(outputFilePath))
            {
                dataStream.CopyTo(outputStream);
            }

            imagesBeingLoaded.Remove(outputFilePath);

            Log.Debug($"[ItemsCache.ResolveImageByUri] Image was saved to file '{outputFilePath}'");
            return new FileInfo(outputFilePath);
        }

        private string ConstructPath(string imageUri)
        {
            var cachePath = CachePath;
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