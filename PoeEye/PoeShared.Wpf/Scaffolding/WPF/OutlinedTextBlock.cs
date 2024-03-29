﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace PoeShared.Scaffolding.WPF;

[ContentProperty("Text")]
public class OutlinedTextBlock : FrameworkElement
{
    public static readonly DependencyProperty TextStrokePositionProperty =
        DependencyProperty.Register("TextStrokePosition",
            typeof(TextStrokePosition),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(TextStrokePosition.Outside, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        "Foreground",
        typeof(Brush),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        "Stroke",
        typeof(Brush),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        "StrokeThickness",
        typeof(double),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        "Text",
        typeof(string),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextInvalidated));

    public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(
        "TextAlignment",
        typeof(TextAlignment),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty TextDecorationsProperty = DependencyProperty.Register(
        "TextDecorations",
        typeof(TextDecorationCollection),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(
        "TextTrimming",
        typeof(TextTrimming),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(OnFormattedTextUpdated));

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
        "TextWrapping",
        typeof(TextWrapping),
        typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(TextWrapping.Wrap, OnFormattedTextUpdated));

    private PathGeometry clipGeometry;
    private FormattedText formattedText;
    private Pen pen;
    private Geometry textGeometry;

    public OutlinedTextBlock()
    {
        UpdatePen();
        TextDecorations = new TextDecorationCollection();
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
    }

    public TextStrokePosition TextStrokePosition
    {
        get => (TextStrokePosition)GetValue(TextStrokePositionProperty);
        set => SetValue(TextStrokePositionProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    [TypeConverter(typeof(FontSizeConverter))]
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public TextDecorationCollection TextDecorations
    {
        get => (TextDecorationCollection)GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => (TextTrimming)GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    private void UpdatePen()
    {
        pen = new Pen(Stroke, StrokeThickness)
        {
            DashCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
        };

        if (TextStrokePosition == TextStrokePosition.Outside || TextStrokePosition == TextStrokePosition.Inside)
        {
            pen.Thickness = StrokeThickness * 2;
        }

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        EnsureGeometry();

        drawingContext.DrawGeometry(Foreground, null, textGeometry);

        if (TextStrokePosition == TextStrokePosition.Outside)
        {
            drawingContext.PushClip(clipGeometry);
        }
        else if (TextStrokePosition == TextStrokePosition.Inside)
        {
            drawingContext.PushClip(textGeometry);
        }

        drawingContext.DrawGeometry(null, pen, textGeometry);

        if (TextStrokePosition == TextStrokePosition.Outside || TextStrokePosition == TextStrokePosition.Inside)
        {
            drawingContext.Pop();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureFormattedText();

        // constrain the formatted text according to the available size

        // the Math.Min call is important - without this constraint (which seems arbitrary, but is the maximum allowable text width), things blow up when availableSize is infinite in both directions
        // the Math.Max call is to ensure we don't hit zero, which will cause MaxTextHeight to throw
        formattedText.MaxTextWidth = Math.Min(3579139, formattedText.MaxTextWidth);
        formattedText.MaxTextHeight = Math.Max(0.0001d, formattedText.MaxTextHeight);

        // return the desired size
        return new Size(Math.Ceiling(formattedText.Width), Math.Ceiling(formattedText.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        EnsureFormattedText();

        // need to re-generate the geometry now that the dimensions have changed
        textGeometry = null;
        UpdatePen();

        return finalSize;
    }

    private static void OnFormattedTextInvalidated(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var outlinedTextBlock = (OutlinedTextBlock)dependencyObject;
        outlinedTextBlock.formattedText = null;
        outlinedTextBlock.textGeometry = null;

        outlinedTextBlock.InvalidateMeasure();
        outlinedTextBlock.InvalidateVisual();
    }

    private static void OnFormattedTextUpdated(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var outlinedTextBlock = (OutlinedTextBlock)dependencyObject;
        outlinedTextBlock.UpdateFormattedText();
        outlinedTextBlock.textGeometry = null;

        outlinedTextBlock.InvalidateMeasure();
        outlinedTextBlock.InvalidateVisual();
    }

    private void EnsureFormattedText()
    {
        if (formattedText != null)
        {
            return;
        }

#pragma warning disable CS0618 // works just fine on an older API, ignore for now
        formattedText = new FormattedText(
            Text ?? string.Empty,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            TextFormattingMode.Ideal);
#pragma warning restore CS0618

        UpdateFormattedText();
    }

    private void UpdateFormattedText()
    {
        if (formattedText == null)
        {
            return;
        }

        formattedText.MaxLineCount = TextWrapping == TextWrapping.NoWrap ? 1 : int.MaxValue;
        formattedText.TextAlignment = TextAlignment;
        formattedText.Trimming = TextTrimming;

        formattedText.SetFontSize(FontSize);
        formattedText.SetFontStyle(FontStyle);
        formattedText.SetFontWeight(FontWeight);
        formattedText.SetFontFamily(FontFamily);
        formattedText.SetFontStretch(FontStretch);
        formattedText.SetTextDecorations(TextDecorations);
    }

    private void EnsureGeometry()
    {
        if (textGeometry != null)
        {
            return;
        }

        EnsureFormattedText();
        textGeometry = formattedText.BuildGeometry(new Point(0, 0));

        if (TextStrokePosition == TextStrokePosition.Outside)
        {
            var boundsGeo = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            clipGeometry = Geometry.Combine(boundsGeo, textGeometry, GeometryCombineMode.Exclude, null);
        }
    }
}