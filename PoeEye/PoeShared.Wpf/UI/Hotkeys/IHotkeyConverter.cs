using JetBrains.Annotations;
using PoeShared.Prism;

namespace PoeShared.UI;

public interface IHotkeyConverter : IConverter<string, HotkeyGesture>, IConverter<HotkeyGesture, string>
{
    [NotNull] 
    string ConvertToString([CanBeNull] HotkeyGesture hotkeyGesture);

    [NotNull] 
    HotkeyGesture ConvertFromString([CanBeNull] string source);
}