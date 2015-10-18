namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.IO;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared;
    using PoeShared.Utilities;

    using ReactiveUI;

    using Utilities;

    internal sealed class ImageViewModel : DisposableReactiveObject
    {
        private readonly ItemsCache cache;
        private readonly Uri imageUri;

        public ImageViewModel(
            [NotNull] ItemsCache cache,
            [CanBeNull] Uri imageUri)
        {
            Guard.ArgumentNotNull(() => cache);
            
            this.cache = cache;
            this.imageUri = imageUri;

            image = new Image();

            IsLoading = true;
            cache
                .ResolveImageByUri(imageUri)
                .ObserveOn(Dispatcher.CurrentDispatcher)
                .Finally(() => IsLoading = false)
                .Subscribe(LoadImage, Log.HandleException)
                .AddTo(Anchors);
        }

        private Image image;

        public Image Image
        {
            get { return image; }
            set { this.RaiseAndSetIfChanged(ref image, value); }
        }

        private bool isLoading;

        public bool IsLoading
        {
            get { return isLoading; }
            set { this.RaiseAndSetIfChanged(ref isLoading, value); }
        }

        private void LoadImage(FileInfo cachedImageFilePath)
        {
            Log.Instance.Debug($"[ImageViewModel] Loading image...\r\n\tBase URI: '{imageUri}'\r\n\tFilePath: '{cachedImageFilePath.FullName}'");
            image.Source = LoadBitmapImage(cachedImageFilePath.FullName);
        }

        private BitmapImage LoadBitmapImage(string filePath)
        {
            var loadedImage = new BitmapImage(new Uri(filePath));
            return loadedImage;
        }

    }
}