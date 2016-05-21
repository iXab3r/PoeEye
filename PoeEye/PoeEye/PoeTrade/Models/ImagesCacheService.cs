﻿namespace PoeEye.PoeTrade.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Http;
    using PoeShared.Prism;

    internal sealed class ImagesCacheService : IImagesCacheService
    {
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);
        private readonly IScheduler uiScheduler;

        private static readonly string CachePath = Environment.ExpandEnvironmentVariables($@"%APPDATA%\PoeEye\Cache\");
        private readonly IFactory<IHttpClient> httpClientFactory;

        private readonly IDictionary<string, IObservable<FileInfo>> imagesBeingLoaded = new ConcurrentDictionary<string, IObservable<FileInfo>>();

        public ImagesCacheService(
            [NotNull] IFactory<IHttpClient> httpClientFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => httpClientFactory);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.httpClientFactory = httpClientFactory;
            this.uiScheduler = uiScheduler;
        }

        public IObservable<FileInfo> ResolveImageByUri(Uri imageUri)
        {
            Log.Instance.Trace($"[ItemsCache.ResolveImageByUri] Resolving image '{imageUri}'...");
            var outputFilePath = ConstructPath(imageUri.AbsolutePath);

            IObservable<FileInfo> inProgress;
            if (imagesBeingLoaded.TryGetValue(outputFilePath, out inProgress))
            {
                Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image is currently loading, returning source");
                return inProgress;
            }

            if (File.Exists(outputFilePath))
            {
                Log.Instance.Trace($"[ItemsCache.ResolveImageByUri] Image is already loaded, cache path '{outputFilePath}'");
                return Observable.Return(new FileInfo(outputFilePath)).ObserveOn(uiScheduler);
            }

            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image '{imageUri}' is not loaded, downloading it...");
            var httpClient = httpClientFactory.Create();
            var result = httpClient
                .GetStreamAsync(imageUri)
                .Select(x => LoadImageFromStream(outputFilePath, x))
                .ObserveOn(uiScheduler)
                .Publish();

            imagesBeingLoaded[outputFilePath] = result;

            result.Connect();

            return result;
        }

        private FileInfo LoadImageFromStream(string outputFilePath, Stream dataStream)
        {
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Starting to download image to cache...\r\n\tFilePath: '{outputFilePath}'");

#if DEBUG
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Atrificial delay: {ArtificialDelay}");
            Thread.Sleep(ArtificialDelay);
#endif

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

            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image was saved to file '{outputFilePath}'");
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