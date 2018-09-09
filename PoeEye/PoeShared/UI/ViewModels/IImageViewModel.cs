using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.UI.ViewModels
{
    public interface IImageViewModel : IDisposableReactiveObject
    {
        BitmapImage ImageSource { [CanBeNull] get; }

        bool IsLoading { get; set; }
    }
}