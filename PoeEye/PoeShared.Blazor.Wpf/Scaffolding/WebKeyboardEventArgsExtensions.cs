using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.UI;

namespace PoeShared.Blazor.Wpf.Scaffolding;

public static class WebKeyboardEventArgsExtensions
{
    public static HotkeyGesture ToHotkeyGesture([NotNull] this KeyboardEventArgs args)
    {
        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        return WebKeyToWpfKeyConverter.Instance.Convert(args);
    }
}