using System.Windows.Input;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IOverlayViewModel : IWindowViewModel
{
    float Opacity { get; set; }

    bool ShowResizeThumbs { get; set; }

    bool IsLocked { get; set; }

    bool IsUnlockable { get; }

    OverlayMode OverlayMode { get; set; }
    
    bool IsFocusable { get; set; }
    
    bool GrowUpwards { get; }

    ICommand LockWindowCommand { [NotNull] get; }

    ICommand UnlockWindowCommand { [NotNull] get; }

    ICommand MakeLayeredCommand { [NotNull] get; }

    ICommand MakeTransparentCommand { [NotNull] get; }

    WpfPoint ViewModelLocation { get; }

    void ResetToDefault();

    void SetActivationController([NotNull] IActivationController controller);
}