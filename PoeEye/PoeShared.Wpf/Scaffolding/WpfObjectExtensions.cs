using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PoeShared.Scaffolding;

public static class WpfObjectExtensions
{
    public static TItem AddTo<TItem>(this TItem instance, IAddChild parent)
    {
        Guard.ArgumentNotNull(instance, nameof(instance));
        Guard.ArgumentNotNull(parent, nameof(parent));

        parent.AddChild(instance);
        return instance;
    }
    
    public static T AsFrozen<T>(this T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
    
    /// <summary>
    /// Forces the value to stay between minimum and maximum.
    /// </summary>
    /// <returns>minimum, if value is less than minimum.
    /// Maximum, if value is greater than maximum.
    /// Otherwise, value.</returns>
    public static double CoerceValue(this double value, double minimum, double maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }

    /// <summary>
    /// Forces the value to stay between minimum and maximum.
    /// </summary>
    /// <returns>minimum, if value is less than minimum.
    /// Maximum, if value is greater than maximum.
    /// Otherwise, value.</returns>
    public static int CoerceValue(this int value, int minimum, int maximum)
    {
        return Math.Max(Math.Min(value, maximum), minimum);
    }
}