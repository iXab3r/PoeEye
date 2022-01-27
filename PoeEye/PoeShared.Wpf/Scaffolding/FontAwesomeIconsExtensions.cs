using FontAwesome.WPF;

namespace PoeShared.Scaffolding;

public static class FontAwesomeIconsExtensions
{
    public static string ToIcon(this FontAwesomeIcon icon)
    {
        return char.ConvertFromUtf32((int)icon);
    }
}