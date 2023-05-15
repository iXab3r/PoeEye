using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PoeShared.Scaffolding;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using NumericUpDown = MahApps.Metro.Controls.NumericUpDown;
using TextBox = System.Windows.Controls.TextBox;

namespace PoeShared.UI;

[TemplatePart(Name = PART_TextBoxName, Type = typeof(TextBox))]
[TemplatePart(Name = PART_IncrementButton, Type = typeof(RepeatButton))]
[TemplatePart(Name = PART_DecrementButton, Type = typeof(RepeatButton))]
public class NumericUpDownEx : NumericUpDown
{
    /// <summary>
    /// Gets the required template name of the textbox portion of this control.
    /// </summary>
    private const string PART_TextBoxName = "PART_TextBox";

    /// <summary>
    /// Gets the required template name of the increment button for this control.
    /// </summary>
    private const string PART_IncrementButton = "PART_NumericUp";

    /// <summary>
    /// Gets the required template name of the decrement button for this control.
    /// </summary>
    private const string PART_DecrementButton = "PART_NumericDown";

    public static readonly DependencyProperty IsMouseDragEnabledProperty = DependencyProperty.Register(
        nameof(IsMouseDragEnabled), typeof(bool), typeof(NumericUpDownEx), new PropertyMetadata(default(bool)));

    public bool IsMouseDragEnabled
    {
        get => (bool) GetValue(IsMouseDragEnabledProperty);
        set => SetValue(IsMouseDragEnabledProperty, value);
    }
    
    static NumericUpDownEx()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDownEx), new FrameworkPropertyMetadata(typeof(NumericUpDownEx)));
    }

    private Point? lastDragPoint;
    private Point? startDragPoint;
    private FrameworkElement incrementButton;
    private FrameworkElement decrementButton;
    
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        incrementButton = GetTemplateChild(PART_IncrementButton) as RepeatButton;
        decrementButton = GetTemplateChild(PART_DecrementButton) as RepeatButton;

        if (incrementButton != null)
        {
            incrementButton.PreviewMouseDown += TextBoxOnMouseDown;
        }
        if (decrementButton != null)
        {
            decrementButton.PreviewMouseDown += TextBoxOnMouseDown;
        }
    }

    private void TextBoxOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement uiElement)
        {
            return;
        }
        
        uiElement.ReleaseMouseCapture();
        uiElement.PreviewMouseMove -= TextBoxOnMouseMove;
        uiElement.PreviewMouseUp -= TextBoxOnMouseUp;
        startDragPoint = lastDragPoint = null;
        uiElement.Cursor = default;
    }

    private void TextBoxOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement uiElement)
        {
            return;
        }
        
        if (IsMouseDragEnabled == false)
        {
            return;
        }

        uiElement.CaptureMouse();
        lastDragPoint = null;
        startDragPoint = Mouse.GetPosition(this);
        uiElement.PreviewMouseMove += TextBoxOnMouseMove;
        uiElement.PreviewMouseUp += TextBoxOnMouseUp;
        uiElement.Cursor = Cursors.ScrollAll;
    }

    private void TextBoxOnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement uiElement)
        {
            return;
        }
        
        if (IsMouseDragEnabled == false)
        {
            return;
        }
        
        if (!uiElement.IsMouseCaptured || 
            startDragPoint == null || 
            Value == null)
        {
            return;
        }

        var position = Mouse.GetPosition(this);

        if (lastDragPoint == null)
        {
            var startDelta = new Point(Math.Abs(position.X - startDragPoint.Value.X), Math.Abs(position.Y - startDragPoint.Value.Y));

            if (startDelta.X > SystemParameters.MinimumHorizontalDragDistance ||
                startDelta.Y > SystemParameters.MinimumVerticalDragDistance)
            {
                lastDragPoint = position;
            }
        }
        else
        {
            var positionDelta = new Point(position.X - lastDragPoint.Value.X,  - position.Y + lastDragPoint.Value.Y);
            var currentValue = Value.Value;

            var totalSteps = positionDelta.X + positionDelta.Y;
            var deltaSteps = Interval * Math.Round(totalSteps / Math.Min(SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance));
            var calculatedValue = currentValue + deltaSteps;
        
            SetCurrentValue(ValueProperty, calculatedValue);
            lastDragPoint = position;
        }
    }
}