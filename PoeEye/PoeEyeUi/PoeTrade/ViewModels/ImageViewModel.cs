namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.IO;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeShared;
    using PoeShared.Scaffolding;

    using Prism;

    using ReactiveUI;

    internal sealed class ImageViewModel : DisposableReactiveObject
    {
        private readonly Uri imageUri;

        private Image image;

        private bool isLoading;

        public ImageViewModel(
            [NotNull] ImagesCache cache,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [CanBeNull] Uri imageUri)
        {
            Guard.ArgumentNotNull(() => cache);

            this.imageUri = imageUri;

            image = new Image();

            IsLoading = true;
            cache
                .ResolveImageByUri(imageUri)
                .ObserveOn(uiScheduler)
                .Finally(() => IsLoading = false)
                .Subscribe(LoadImage, Log.HandleException)
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

        private void LoadImage(FileInfo cachedImageFilePath)
        {
            Log.Instance.Trace($"[ImageViewModel] Loading image...\r\n\tBase URI: '{imageUri}'\r\n\tFilePath: '{cachedImageFilePath.FullName}'");
            try
            {
                var bitmap = LoadBitmapImage(cachedImageFilePath.FullName);
                image.Source = bitmap;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
            }
        }

        private BitmapImage LoadBitmapImage(string filePath)
        {
            var loadedImage = new BitmapImage(new Uri(filePath));
            return loadedImage;
        }
    }
}