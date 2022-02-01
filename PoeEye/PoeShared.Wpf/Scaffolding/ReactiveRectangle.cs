using PoeShared.Scaffolding;
using System;
using System.Drawing;
using PoeShared.Bindings;
using PoeShared.Logging;
using PoeShared.Services;
using PropertyChanged;

namespace EyeAuras.OnTopReplica;

public sealed class ReactiveRectangle : BindableReactiveObject
{
    private readonly NamedLock updateLock;
    
    private Rectangle bounds;

    public ReactiveRectangle()
    {
        updateLock = new NamedLock(nameof(ReactiveRectangle));
    }

    public ReactiveRectangle(Rectangle rectangle) : this()
    {
        SetValue(rectangle);
    }

    [DoNotNotify]
    public Rectangle Bounds
    {
        get => bounds;
        set => SetValue(value);
    }

    [DoNotNotify]
    public Point Location
    {
        get => bounds.Location;
        set => SetValue(new Rectangle(value, bounds.Size));
    }

    [DoNotNotify]
    public Size Size
    {
        get => bounds.Size;
        set => SetValue(new Rectangle(bounds.Location, value));
    }

    [DoNotNotify]
    public int RegionX
    {
        get => bounds.X;
        set => SetValue(new Rectangle(value, bounds.Y, bounds.Width, bounds.Height));
    }

    [DoNotNotify]
    public int RegionY
    {
        get => bounds.Y;
        set => SetValue(new Rectangle(bounds.X, value, bounds.Width, bounds.Height));
    }

    [DoNotNotify]
    public int RegionWidth
    {
        get => bounds.Width;
        set => SetValue(new Rectangle(bounds.X, bounds.Y, Math.Max(0, value), bounds.Height));
    }

    [DoNotNotify]
    public int RegionHeight
    {
        get => bounds.Height;
        set => SetValue(new Rectangle(bounds.X, bounds.Y, bounds.Width, Math.Max(0, value)));
    }

    public void Reset()
    {
        SetValue(Rectangle.Empty);
    }

    public void SetValue(Rectangle newValue)
    {
        using var @lock = updateLock.Enter();
        
        var previousValue = bounds;
        if (previousValue == newValue)
        {
            return;
        }

        bounds = newValue;
        this.RaisePropertyChanged(nameof(Bounds));
        this.RaiseIfChanged(nameof(Location), previousValue.Location, newValue.Location);
        this.RaiseIfChanged(nameof(Size), previousValue.Size, newValue.Size);
        this.RaiseIfChanged(nameof(RegionX), previousValue.X, newValue.X);
        this.RaiseIfChanged(nameof(RegionY), previousValue.Y, newValue.Y);
        this.RaiseIfChanged(nameof(RegionWidth), previousValue.Width, newValue.Width);
        this.RaiseIfChanged(nameof(RegionHeight), previousValue.Height, newValue.Height);
    }
        
    public override string ToString()
    {
        return $"Region({bounds})";
    }
}