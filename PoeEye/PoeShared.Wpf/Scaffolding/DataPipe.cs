using System.Windows;

namespace PoeShared.Scaffolding;

public class DataPipe : Freezable
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            "Source",
            typeof(object),
            typeof(DataPipe),
            new FrameworkPropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            "Target",
            typeof(object),
            typeof(DataPipe),
            new FrameworkPropertyMetadata(null));

    public object Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public object Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    protected override Freezable CreateInstanceCore()
    {
        return new DataPipe();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DataPipe) d).OnSourceChanged(e);
    }

    protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        Target = e.NewValue;
    }
}