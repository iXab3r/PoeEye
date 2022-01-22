using System.Windows.Media;

namespace PoeShared.Scaffolding;

public static class ColorExtensions
{
    public static Color InterpolateTo(this Color from, Color to, double progress)
    {
        return from + (to - from) * (float) progress;
    }
}