using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using System;

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
        private static long GlobalIdx;
        private readonly string controlId = $"Content#{Interlocked.Increment(ref GlobalIdx)}";

        static CachedContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CachedContentControl), new FrameworkPropertyMetadata(typeof(CachedContentControl)));
        }

        public CachedContentControl()
        {
            Log = GetType().PrepareLogger().WithSuffix(controlId)
                .WithSuffix(() => Name != null ? $"Name: {Name}" : null)
                .WithSuffix(() => ContentPath != null ? $"Path: {ContentPath}" : null)
                .WithSuffix(() => $"DataContext: {DataContext ?? "NULL"}")
                .WithSuffix(() => $"Content: {Content ?? "NULL"}");
            Log.Debug(() => "Created");

            this.Observe(ContentProperty)
                .Select(x => this.GetBindingExpression(ContentProperty))
                .Select(x => x?.ParentBinding?.Path?.Path.ToString())
                .Subscribe(x => ContentPath = x);
            
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private string ContentPath { get; set; }

        private IFluentLog Log { get; }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Log.Debug(() => "Unloaded");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Log.Debug(() => "Loaded");
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Log.Debug(() => $"Applied template {Template}");
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            LogChange("Template", oldTemplate, newTemplate);
        }

        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);
            LogChange("Content template", oldContentTemplate, newContentTemplate);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            LogChange("Content", oldContent, newContent);
        }

        protected override void OnContentTemplateSelectorChanged(DataTemplateSelector oldContentTemplateSelector, DataTemplateSelector newContentTemplateSelector)
        {
            base.OnContentTemplateSelectorChanged(oldContentTemplateSelector, newContentTemplateSelector);
            LogChange("Content template selector", oldContentTemplateSelector, newContentTemplateSelector);
        }

        private void LogChange(string name, object oldValue, object newValue)
        {
            if (!Log.IsDebugEnabled)
            {
                return;
            }
            if (oldValue != null && newValue != null)
            {
                Log.Debug($"{name} has been changed: {oldValue} => {newValue}");
            } else if (oldValue == null && newValue != null)
            {
                Log.Debug($"{name} has been assigned: {newValue}");
            } else if (oldValue == null)
            {
                Log.Debug($"{name} has been reset");
            }
            else
            {
                Log.Debug($"{name} has been reset, old value: {oldValue}");
            }
        }
    }
}