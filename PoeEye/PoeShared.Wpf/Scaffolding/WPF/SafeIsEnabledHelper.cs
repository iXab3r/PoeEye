using System;
using System.ComponentModel;
using System.Windows;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

/// <summary>
/// Provides a safe replacement for <see cref="UIElement.IsEnabled"/> bindings that can
/// trigger <see cref="System.ComponentModel.Win32Exception"/> (error 534,
/// “Arithmetic result exceeded 32 bits”) inside WPF when disabling controls
/// that host native HWNDs (e.g., <see cref="System.Windows.Interop.HwndHost"/> or
/// <see cref="System.Windows.Forms.Integration.WindowsFormsHost"/>).
/// 
/// This is a long-standing WPF issue where <see cref="MS.Win32.UnsafeNativeMethods.EnableWindow"/>
/// may set <c>GetLastError</c> to a stale non-zero value even on success, causing WPF to
/// throw a misleading <see cref="Win32Exception"/>.  See discussion:
/// - https://github.com/dotnet/wpf/issues/836
/// - https://stackoverflow.com/questions/37210524/wpf-enablewindow-win32exception-arithmetic-result-exceeded-32-bits
///
/// The attached property mirrors <see cref="UIElement.IsEnabled"/> but applies the change
/// inside a guarded <c>try/catch</c> that swallows and logs only the spurious Win32 error.
/// Use this attached property instead of directly binding to <c>IsEnabled</c> whenever a
/// control’s subtree may include HWND-based hosts.
///
/// Example usage in XAML:
/// <code>
/// <eye:CachedContentControl
///     local:SafeIsEnabledBehavior.IsEnabled="{Binding IsReadOnly, Converter={StaticResource NotConverter}}"
///     Content="{Binding ValueEditor}" />
/// </code>
/// </summary>
public static class SafeIsEnabledHelper
{
    private static readonly IFluentLog Log = typeof(SafeIsEnabledHelper).PrepareLogger();

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SafeIsEnabledHelper),
            new PropertyMetadata(true, OnSafeIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value)
        => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(DependencyObject element)
        => (bool)element.GetValue(IsEnabledProperty);

    private static void OnSafeIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            try
            {
                element.IsEnabled = (bool)e.NewValue;
            }
            catch (Win32Exception w32) when (w32.NativeErrorCode == 534)
            {
                Log.Warn($"Swallowed Win32Exception 534 on {element.GetType().Name} (likely harmless EnableWindow issue).");
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected exception while toggling IsEnabled on {element.GetType().Name}.", ex);
                throw;
            }
        }
    }
}