using System;
using System.Drawing;
using System.Windows.Forms;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.WinForms.Services;

internal sealed class ControlLocationTracker : DisposableReactiveObjectWithLogger, IBlazorControlLocationTracker
{
    private readonly Control control;
    private Rectangle boundsOnScreen;
    private Form? parentForm;

    public ControlLocationTracker(Control control)
    {
        this.control = control;

        control.HandleCreated += ControlOnHandleCreated;
        control.HandleDestroyed += ControlOnHandleDestroyed;
        control.LocationChanged += ControlOnBoundsChanged;
        control.SizeChanged += ControlOnBoundsChanged;
        control.VisibleChanged += ControlOnBoundsChanged;
        control.ParentChanged += ControlOnParentChanged;

        Anchors.Add(() =>
        {
            DetachFromParentForm();
            control.HandleCreated -= ControlOnHandleCreated;
            control.HandleDestroyed -= ControlOnHandleDestroyed;
            control.LocationChanged -= ControlOnBoundsChanged;
            control.SizeChanged -= ControlOnBoundsChanged;
            control.VisibleChanged -= ControlOnBoundsChanged;
            control.ParentChanged -= ControlOnParentChanged;
        });

        AttachToParentForm(control.FindForm());
        UpdateBounds();
    }

    public Rectangle BoundsOnScreen
    {
        get => boundsOnScreen;
        private set => RaiseAndSetIfChanged(ref boundsOnScreen, value);
    }

    private void ControlOnHandleCreated(object? sender, EventArgs e)
    {
        AttachToParentForm(control.FindForm());
        UpdateBounds();
    }

    private void ControlOnHandleDestroyed(object? sender, EventArgs e)
    {
        BoundsOnScreen = Rectangle.Empty;
    }

    private void ControlOnParentChanged(object? sender, EventArgs e)
    {
        AttachToParentForm(control.FindForm());
        UpdateBounds();
    }

    private void ControlOnBoundsChanged(object? sender, EventArgs e)
    {
        UpdateBounds();
    }

    private void ParentFormOnBoundsChanged(object? sender, EventArgs e)
    {
        UpdateBounds();
    }

    private void AttachToParentForm(Form? form)
    {
        if (ReferenceEquals(parentForm, form))
        {
            return;
        }

        DetachFromParentForm();
        parentForm = form;
        if (parentForm == null)
        {
            return;
        }

        parentForm.LocationChanged += ParentFormOnBoundsChanged;
        parentForm.SizeChanged += ParentFormOnBoundsChanged;
        parentForm.FormClosed += ParentFormOnClosed;
    }

    private void DetachFromParentForm()
    {
        if (parentForm == null)
        {
            return;
        }

        parentForm.LocationChanged -= ParentFormOnBoundsChanged;
        parentForm.SizeChanged -= ParentFormOnBoundsChanged;
        parentForm.FormClosed -= ParentFormOnClosed;
        parentForm = null;
    }

    private void ParentFormOnClosed(object? sender, FormClosedEventArgs e)
    {
        UpdateBounds();
    }

    private void UpdateBounds()
    {
        try
        {
            if (!control.IsHandleCreated || control.IsDisposed)
            {
                BoundsOnScreen = Rectangle.Empty;
                return;
            }

            var topLeft = control.PointToScreen(Point.Empty);
            BoundsOnScreen = new Rectangle(topLeft, control.ClientSize);
        }
        catch (Exception e)
        {
            Log.Warn("Failed to calculate bounds of a WinForms control", e);
        }
    }
}
