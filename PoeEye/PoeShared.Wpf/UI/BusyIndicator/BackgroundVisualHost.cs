using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

/// <summary>
///     This class provides helper methods to track background visuals
/// </summary>
internal static class BackgroundVisualHost
{
    private static readonly Dictionary<FrameworkElement, List<Guid>> ChildMap = new();
    private static readonly Dictionary<Guid, AddedChildHolder> ChildHolders = new();

    private static readonly DependencyPropertyKey ElementIdPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "ElementId",
        typeof(Guid),
        typeof(BackgroundVisualHost),
        new FrameworkPropertyMetadata(Guid.Empty));

    private static readonly DependencyProperty ElementIdProperty = ElementIdPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey BackgroundHostPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "BackgroundHost",
        typeof(WindowBackgroundVisualHost),
        typeof(WindowBackgroundVisualHost),
        new FrameworkPropertyMetadata(null));

    private static readonly DependencyProperty BackgroundHostProperty = BackgroundHostPropertyKey.DependencyProperty;

    [AttachedPropertyBrowsableForType(typeof(UIElement))]
    private static Guid GetElementId(UIElement obj)
    {
        return (Guid) obj.GetValue(ElementIdProperty);
    }

    private static void SetElementId(UIElement obj, Guid value)
    {
        obj.SetValue(ElementIdPropertyKey, value);
    }

    [AttachedPropertyBrowsableForType(typeof(UIElement))]
    private static WindowBackgroundVisualHost GetBackgroundHost(UIElement obj)
    {
        return (WindowBackgroundVisualHost) obj.GetValue(BackgroundHostProperty);
    }

    private static void SetBackgroundHost(UIElement obj, WindowBackgroundVisualHost value)
    {
        obj.SetValue(BackgroundHostPropertyKey, value);
    }

    private static WindowBackgroundVisualHost GetHost(UIElement element)
    {
        var root = element.VisualAncestors().OfType<UIElement>().LastOrDefault();
        if (root == null)
        {
            return null;
        }

        var host = GetBackgroundHost(root);
        if (host != null)
        {
            return host;
        }

        var decorator = root.VisualDescendants().OfType<AdornerDecorator>().FirstOrDefault();
        if (decorator?.Child == null)
        {
            return null;
        }

        host = new WindowBackgroundVisualHost(decorator.Child);
        if (decorator is BusyAdornerDecorator adornerDecorator)
        {
            adornerDecorator.BusyIndicatorHost = host;
        }
        else
        {
            decorator.AdornerLayer.Add(host);
        }

        SetBackgroundHost(root, host);

        return host;
    }

    public static Guid AddChild(FrameworkElement parent, Func<UIElement> createElement)
    {
        var id = Guid.NewGuid();

        // this will either add the child now, or wait until it is loaded
        ChildHolders[id] = new AddedChildHolder(parent, createElement, id);

        return id;
    }

    public static void RemoveChild(FrameworkElement parent, Guid elementId)
    {
        if (!ChildHolders.TryGetValue(elementId, out var holder))
        {
            return;
        }

        holder.Detach();
        ChildHolders.Remove(elementId);
    }

    public static void WindowPositionChanged(FrameworkElement parent, Point point)
    {
        var host = GetHost(parent);
        if (host == null)
        {
            return;
        }

        if (!ChildMap.TryGetValue(parent, out var children))
        {
            return;
        }

        foreach (var id in children)
        {
            host.Move(id, point);
        }
    }

    public static void DispatchAction(FrameworkElement parent, Guid id, Action<UIElement> action)
    {
        var host = GetHost(parent);
        host?.PerformAction(id, action);
    }

    private static void HandleAdornedSizeChanged(object sender)
    {
        var element = sender as FrameworkElement;
        var host = GetHost(element);
        if (host == null || element == null)
        {
            return;
        }

        var size = element.RenderSize;
        foreach (var id in ChildMap[element])
        {
            host.Resize(id, size);
        }
    }

    private class AddedChildHolder : IWeakEventListener
    {
        private readonly Func<UIElement> createElement;
        private readonly Guid id;
        private readonly FrameworkElement parent;

        public AddedChildHolder(FrameworkElement parent, Func<UIElement> createElement, Guid id)
        {
            this.parent = parent;
            this.createElement = createElement;
            this.id = id;

            IsVisibleWeakEventManager.AddListener(parent, this);
            LoadedWeakEventManager.AddListener(parent, this);

            if (parent.IsVisible)
            {
                AddChild();
            }
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(IsVisibleWeakEventManager))
            {
                IsVisibleChanged((EventArgs<bool>) e);
                return true;
            }

            if (managerType == typeof(SizeChangedWeakEventManager))
            {
                HandleAdornedSizeChanged(sender);
                return true;
            }

            if (managerType != typeof(LoadedWeakEventManager))
            {
                return false;
            }

            Loaded();
            return true;

        }

        private void Loaded()
        {
            if (parent.IsVisible)
            {
                AddChild();
            }
        }

        private void IsVisibleChanged(EventArgs<bool> e)
        {
            if (e.Value)
            {
                if (parent.IsLoaded)
                {
                    AddChild();
                }
            }
            else
            {
                SizeChangedWeakEventManager.RemoveListener(parent, this);
                RemoveChild();
            }
        }

        public void Detach()
        {
            RemoveChild();
            IsVisibleWeakEventManager.RemoveListener(parent, this);
            SizeChangedWeakEventManager.RemoveListener(parent, this);
            LoadedWeakEventManager.RemoveListener(parent, this);
        }

        public void InvalidateArrange()
        {
            parent.InvalidateArrange();
        }

        private void AddChild()
        {
            var host = GetHost(parent);
            var root = parent.VisualAncestors().OfType<UIElement>().LastOrDefault();
            if (root == null)
            {
                return;
            }

            var xForm = parent.TransformToAncestor(root);
            var bounds = xForm.TransformBounds(new Rect(parent.RenderSize));

            host?.AddChild(createElement, id, bounds);

            if (!ChildMap.TryGetValue(parent, out var children))
            {
                children = new List<Guid>();
                ChildMap.Add(parent, children);
            }

            SizeChangedWeakEventManager.AddListener(parent, this);
            children.Add(id);
        }

        private void RemoveChild()
        {
            var host = GetHost(parent);
            host?.RemoveChild(id);

            if (ChildMap.TryGetValue(parent, out var children))
            {
                children.Remove(id);
                if (children.Count == 0)
                {
                    ChildMap.Remove(parent);
                }
            }
        }
    }

    private sealed class WindowBackgroundVisualHost : Adorner
    {
        private readonly HashSet<Guid> addedChildren = new();
        private readonly Thread backgroundThread;
        private readonly HostVisual childHost = new();
        private readonly AutoResetEvent sync = new(false);
        private Dispatcher backgroundDispatcher;
        private Canvas root;

        public WindowBackgroundVisualHost(UIElement adorned)
            : base(adorned)
        {
            // make sure this is on top of everything else
            Panel.SetZIndex(this, int.MaxValue);
            backgroundThread = new Thread(CreateAndRun);
            backgroundThread.SetApartmentState(ApartmentState.STA);
            backgroundThread.Name = "BackgroundVisualHostThread";
            backgroundThread.IsBackground = true;
            backgroundThread.Start();

            AddLogicalChild(childHost);
            AddVisualChild(childHost);

            sync.WaitOne();
        }

        protected override IEnumerator LogicalChildren
        {
            get { yield return childHost; }
        }

        protected override int VisualChildrenCount => 1;

        public void AddChild(Func<UIElement> createElement, Guid id, Rect bounds)
        {
            BeginInvoke(
                () =>
                {
                    if (!addedChildren.Add(id))
                    {
                        return;
                    }

                    var child = createElement();
                    SetElementId(child, id);
                    root.Children.Add(child);

                    Canvas.SetLeft(child, bounds.X);
                    Canvas.SetTop(child, bounds.Y);
                    child.SetCurrentValue(WidthProperty, bounds.Width);
                    child.SetCurrentValue(HeightProperty, bounds.Height);
                });
        }

        public void RemoveChild(Guid id)
        {
            PerformAction(id, child =>
            {
                addedChildren.Remove(id);
                root.Children.Remove(child);
            });
        }

        public void Resize(Guid id, Size s)
        {
            PerformAction(id,
                child =>
                {
                    child.SetCurrentValue(WidthProperty, s.Width);
                    child.SetCurrentValue(HeightProperty, s.Height);
                });
        }

        public void Move(Guid id, Point p)
        {
            PerformAction(id,
                child =>
                {
                    Canvas.SetLeft(child, p.X);
                    Canvas.SetTop(child, p.Y);
                });
        }

        public void PerformAction(Guid id, Action<UIElement> action)
        {
            BeginInvoke(() =>
            {
                var child = root.Children
                    .OfType<UIElement>()
                    .FirstOrDefault(c => GetElementId(c) == id);

                if (child != null)
                {
                    action(child);
                }
            });
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var id in addedChildren)
            {
                if (ChildHolders.TryGetValue(id, out var holder))
                {
                    holder.InvalidateArrange();
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
            {
                return childHost;
            }

            throw new IndexOutOfRangeException();
        }

        private void BeginInvoke(Action act)
        {
            backgroundDispatcher.BeginInvoke(act);
        }

        private void CreateAndRun()
        {
            backgroundDispatcher = Dispatcher.CurrentDispatcher;
            var source = new VisualTargetPresentationSource(childHost);
            root = new Canvas();
            sync.Set();
            source.RootVisual = root;

            Dispatcher.Run();
            source.Dispose();
        }
    }
}