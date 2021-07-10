using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace PoeShared.UI
{
    [DefaultProperty("Content")]
    [ContentProperty("Content")]
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public class CachedContentControl : ReactiveControl
    {
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(object), typeof(CachedContentControl), new FrameworkPropertyMetadata(null, OnContentChanged));

        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
            "ContentTemplate", typeof(DataTemplate), typeof(CachedContentControl), new PropertyMetadata(default(DataTemplate)));

        public static readonly DependencyProperty CachedContentProperty = DependencyProperty.Register(
            "CachedContent", typeof(object), typeof(CachedContentControl), new PropertyMetadata(default(object)));

        static CachedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CachedContentControl), new FrameworkPropertyMetadata(typeof(CachedContentControl)));
        }

        public object CachedContent
        {
            get => GetValue(CachedContentProperty);
            set => SetValue(CachedContentProperty, value);
        }

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public DataTemplate ContentTemplate
        {
            get => (DataTemplate) GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (CachedContentControl) d;
            if (e.NewValue != null)
            {
                d.SetValue(CachedContentProperty, e.NewValue);
            }
        }
    }
}