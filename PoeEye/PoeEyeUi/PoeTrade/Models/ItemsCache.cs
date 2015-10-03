namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using PoeShared;

    internal sealed class ItemsCache
    {
        private readonly string CacheFolderName = "Cache";

        private readonly SHA256Managed hashManaged = new SHA256Managed();

        private readonly IDictionary<string, IObservable<FileInfo>> loadingImages = new Dictionary<string, IObservable<FileInfo>>();

        public IObservable<FileInfo> ResolveImageByUri(Uri imageUri)
        {
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Resolving image '{imageUri}'...");
            var outputFilePath = ConstructPath(imageUri.AbsolutePath);

            IObservable<FileInfo> inProgress;
            if (loadingImages.TryGetValue(outputFilePath, out inProgress))
            {
                Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image is currently loading, returning source");
                return inProgress;
            }

            if (File.Exists(outputFilePath))
            {
                Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image is already loaded, cache path '{outputFilePath}'");
                return Observable.Return(new FileInfo(outputFilePath));
            }

            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Image '{imageUri}' is not loaded, downloading it...");
            var httpClient = new HttpClient();
            var result = httpClient
                .GetStreamAsync(imageUri)
                .ToObservable()
                .Select(x => LoadImageFromStream(outputFilePath, x))
                .Publish();

            loadingImages[outputFilePath] = result;

            result.Connect();

            return result;
        }

        private FileInfo LoadImageFromStream(string outputFilePath, Stream dataStream)
        {
            Log.Instance.Debug($"[ItemsCache.ResolveImageByUri] Starting downloading image to cache...\r\n\tFilePath: '{outputFilePath}'");
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

        public static string ToBase64String(String source)
        {
            return Convert.ToBase64String(Encoding.Unicode.GetBytes(source));
        }

        static string Sha256(string password)
        {
            SHA256Managed crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(password), 0, Encoding.ASCII.GetByteCount(password));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }
    }
}