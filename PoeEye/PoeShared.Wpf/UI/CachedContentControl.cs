using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace PoeShared.UI
{
    /// <summary>
    ///   Default content control reloads template when bound to some value, then to NULL and then back.
    ///   This one does not - it hides itself via Visibility when Content is changed to NULL thus making WPF to keep loaded template
    /// </summary>
    [DefaultProperty("Content")]
    [ContentProperty("Content")]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public class CachedContentControl : ContentControl
    {
        static CachedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CachedContentControl), new FrameworkPropertyMetadata(typeof(CachedContentControl)));
        }
    }
}