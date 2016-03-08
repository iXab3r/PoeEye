namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.IO;
    using System.Reactive.Linq;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    internal sealed class ImageViewModel : DisposableReactiveObject
    {
        private Image image = new Image();

        private bool isLoading;

        public ImageViewModel(
            [NotNull] IImagesCacheService cacheService,
            [CanBeNull] Uri imageUri)
        {
            Guard.ArgumentNotNull(() => cacheService);

            if (imageUri == null)
            {
                return;
            }

            IsLoading = true;
            cacheService
                .ResolveImageByUri(imageUri)
                .Finally(() => IsLoading = false)
                .Subscribe(filePath => LoadImage(filePath, imageUri), Log.HandleException)
                .AddTo(Anchors);
        }

        public Image Image
        {
            get { return image; }
            set { this.RaiseAndSetIfChanged(ref image, value); }
        }

        public bool IsLoading
        {
            get { return isLoading; }
            set { this.RaiseAndSetIfChanged(ref isLoading, value); }
        }

        private void LoadImage(FileInfo cachedImageFilePath, Uri imageUri)
        {
            Log.Instance.Trace($"[ImageViewModel] Loading image...\r\n\tBase URI: '{imageUri}'\r\n\tFilePath: '{cachedImageFilePath.FullName}'");
            try
            {
                var bitmap = LoadBitmapImage(cachedImageFilePath.FullName);
                image.Source = bitmap;
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