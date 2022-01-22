using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

[ContentProperty("Template")]
public sealed class TemplateMapping
{
    public string Key { get; set; }

    public DataTemplate Template { get; set; }
}

[ContentProperty("DataTemplates")]
public class MappingTemplateSelector : DataTemplateSelector
{
    private static readonly IFluentLog Log = typeof(MappingTemplateSelector).PrepareLogger();

    private readonly Dictionary<string, DataTemplate> templateLookup;

    public MappingTemplateSelector()
    {
        DataTemplates = new ObservableCollection<TemplateMapping>();
        DataTemplates.CollectionChanged += DataTemplates_CollectionChanged;

        templateLookup = new Dictionary<string, DataTemplate>();
    }

    public ObservableCollection<TemplateMapping> DataTemplates { get; }

    public string KeyPropertyPath { get; set; }

    private void DataTemplates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        IList convertersToProcess = null;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                convertersToProcess = e.NewItems;
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (TemplateMapping maping in e.OldItems)
                {
                    templateLookup.Remove(maping.Key);
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                templateLookup.Clear();
                convertersToProcess = DataTemplates;
                break;
        }

        if (convertersToProcess == null || convertersToProcess.Count <= 0)
        {
            return;
        }

        foreach (TemplateMapping mapping in convertersToProcess)
        {
            templateLookup.Add(mapping.Key, mapping.Template);
        }
    }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (!(container is FrameworkElement element) || item == null)
        {
            return null;
        }

        var key = EvaluatePropertyPath(item, KeyPropertyPath);
        if (key == null)
        {
            return null;
        }

        if (item is INotifyPropertyChanged reactiveItem)
        {
            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    handler => reactiveItem.PropertyChanged += handler, handler => reactiveItem.PropertyChanged -= handler)
                .Where(x => x.EventArgs.PropertyName == KeyPropertyPath)
                .Take(1)
                .SubscribeSafe(
                    () =>
                    {
                        var presenter = (ContentPresenter) container;
                        presenter.ContentTemplateSelector = null;
                        presenter.ContentTemplateSelector = this;
                    }, Log.HandleUiException);
        }

        return templateLookup.TryGetValue(key, out var templateKey)
            ? templateKey
            : null;
    }

    private static string EvaluatePropertyPath(object obj, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var curObj = obj;
        foreach (var item in path.Split('.'))
        {
            var property = curObj.GetType().GetProperty(item);
            if (property != null)
            {
                curObj = property.GetValue(curObj, null);
                if (curObj == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return curObj.ToString();
    }
}