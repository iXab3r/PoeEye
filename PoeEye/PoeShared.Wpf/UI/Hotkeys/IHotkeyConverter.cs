using JetBrains.Annotations;

namespace PoeShared.UI.Hotkeys
{
    public interface IHotkeyConverter
    {
        [NotNull] 
        string ConvertToString([CanBeNull] HotkeyGesture hotkeyGesture);

        [NotNull] 
        HotkeyGesture ConvertFromString([CanBeNull] string source);
    }
}