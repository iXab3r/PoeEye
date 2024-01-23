using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

/// <summary>
///   Default content control reloads template when bound to some value, then to NULL and then back.
///   This one does not - it hides itself via Visibility when Content is changed to NULL thus making WPF to keep loaded template
/// </summary>
[DefaultProperty("Content")]
[ContentProperty("Content")]
[Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
public class CachedContentControl : ContentControl
{
    private static long GlobalIdx;
    private readonly string controlId = $"Content#{Interlocked.Increment(ref GlobalIdx)}";

    static CachedContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CachedContentControl), new FrameworkPropertyMetadata(typeof(CachedContentControl)));
    }
}