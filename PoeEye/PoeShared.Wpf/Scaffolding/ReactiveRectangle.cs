using System;
using System.Diagnostics;
using System.Drawing;
using PoeShared.Bindings;
using PoeShared.Services;
using PropertyChanged;

namespace PoeShared.Scaffolding;

[DebuggerDisplay("Bounds={Bounds} Name={Name}")]
public sealed class ReactiveRectangle : BindableReactiveObject
{
    private readonly NamedLock updateLock;
    
    private Rectangle bounds;

    public ReactiveRectangle(NamedLock updateLock)
    {
        this.updateLock = updateLock;
    }

    public ReactiveRectangle() : this(new NamedLock(nameof(ReactiveRectangle)))
    {
    }

    public ReactiveRectangle(Rectangle rectangle) : this()
    {
        SetValue(rectangle);
    }
    
    public string Name { get; set; }

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
        set => SetValue(bounds with {X = value});
    }

    [DoNotNotify]
    public int RegionY
    {
        get => bounds.Y;
        set => SetValue(bounds with {Y = value});
    }

    [DoNotNotify]
    public int RegionWidth
    {
        get => bounds.Width;
        set => SetValue(bounds with {Width = value});
    }

    [DoNotNotify]
    public int RegionHeight
    {
        get => bounds.Height;
        set => SetValue(bounds with {Height = value});
    }

    public void Reset()
    {
        SetValue(Rectangle.Empty);
    }
    
    public bool SetValue(Rectangle newValue)
    {
        using var @lock = updateLock.Enter();
        
        var previousValue = bounds;
        if (previousValue == newValue)
        {
            return false;
        }

        bounds = newValue;
        this.RaisePropertyChanged(nameof(Bounds));
        this.RaiseIfChanged(nameof(Location), previousValue.Location, newValue.Location);
        this.RaiseIfChanged(nameof(Size), previousValue.Size, newValue.Size);
        this.RaiseIfChanged(nameof(RegionX), previousValue.X, newValue.X);
        this.RaiseIfChanged(nameof(RegionY), previousValue.Y, newValue.Y);
        this.RaiseIfChanged(nameof(RegionWidth), previousValue.Width, newValue.Width);
        this.RaiseIfChanged(nameof(RegionHeight), previousValue.Height, newValue.Height);
        return true;
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("Region");
        builder.AppendParameterIfNotDefault(nameof(Name), Name);
        builder.AppendParameter(nameof(Bounds), bounds);
    }
}