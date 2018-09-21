using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Media.Imaging;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.UI.Models;
using ReactiveUI;

namespace PoeShared.UI.ViewModels
{
    internal sealed class ImageViewModel : DisposableReactiveObject, IImageViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger<ImageViewModel>();
        
        private BitmapImage imageSource;
        private bool isLoading;

        public ImageViewModel(
            [NotNull] IImagesCacheService cacheService,
            [CanBeNull] Uri imageUri)
        {
            Guard.ArgumentNotNull(cacheService, nameof(cacheService));

            if (imageUri == null)
            {
                return;
            }

            IsLoading = true;
            cacheService
                .ResolveImageByUri(imageUri)
                .Take(1)
                .Finally(() => IsLoading = false)
                .Subscribe(filePath => LoadImage(filePath, imageUri), Log.HandleException)
                .AddTo(Anchors);
        }

        public BitmapImage ImageSource
        {
            get => imageSource;
            set => this.RaiseAndSetIfChanged(ref imageSource, value);
        }

        public bool IsLoading
        {
            get => isLoading;
            set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }

        private void LoadImage(FileInfo cachedImageFilePath, Uri imageUri)
        {
            Log.Trace($"[ImageViewModel] Loading image...\r\n\tBase URI: '{imageUri}'\r\n\tFilePath: '{cachedImageFilePath.FullName}'");
            try
            {
                var bitmap = LoadBitmapImage(cachedImageFilePath.FullName);
                ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
            }
        }

        private BitmapImage LoadBitmapImage(string filePath)
        {
            var loadedImage = new BitmapImage(new Uri(filePath));
            return loadedImage;
        }
    }
}