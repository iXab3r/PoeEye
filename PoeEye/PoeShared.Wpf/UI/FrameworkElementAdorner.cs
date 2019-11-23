using System;
using System.Collections;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PoeShared.UI
{
    public class FrameworkElementAdorner : Adorner
    {
        //
        // The framework element that is the adorner. 
        //
        private readonly FrameworkElement child;

        //
        // Placement of the child.
        //
        private readonly AdornerPlacement horizontalAdornerPlacement = AdornerPlacement.Inside;
        private readonly AdornerPlacement verticalAdornerPlacement = AdornerPlacement.Inside;

        //
        // Offset of the child.
        //
        private double offsetX = 0.0;
        private double offsetY = 0.0;

        //
        // Position of the child (when not set to NaN).
        //

        public FrameworkElementAdorner(FrameworkElement adornerChildElement, FrameworkElement adornedElement)
            : base(adornedElement)
        {
            this.child = adornerChildElement;

            base.AddLogicalChild(adornerChildElement);
            base.AddVisualChild(adornerChildElement);
        }

        public FrameworkElementAdorner(FrameworkElement adornerChildElement, FrameworkElement adornedElement,
            AdornerPlacement horizontalAdornerPlacement, AdornerPlacement verticalAdornerPlacement,
            double offsetX, double offsetY)
            : base(adornedElement)
        {
            this.child = adornerChildElement;
            this.horizontalAdornerPlacement = horizontalAdornerPlacement;
            this.verticalAdornerPlacement = verticalAdornerPlacement;
            this.offsetX = offsetX;
            this.offsetY = offsetY;

            adornedElement.SizeChanged += new SizeChangedEventHandler(adornedElement_SizeChanged);

            base.AddLogicalChild(adornerChildElement);
            base.AddVisualChild(adornerChildElement);
        }

        /// <summary>
        /// Event raised when the adorned control's size has changed.
        /// </summary>
        private void adornedElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateMeasure();
        }

        //
        // Position of the child (when not set to NaN).
        //
        public double PositionX { get; set; } = Double.NaN;

        public double PositionY { get; set; } = Double.NaN;

        protected override Size MeasureOverride(Size constraint)
        {
            this.child.Measure(constraint);
            return this.child.DesiredSize;
        }

        /// <summary>
        /// Determine the X coordinate of the child.
        /// </summary>
        private double DetermineX()
        {
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                {
                    if (horizontalAdornerPlacement == AdornerPlacement.Outside)
                    {
                        return -child.DesiredSize.Width + offsetX;
                    }
                    else
                    {
                        return offsetX;
                    }
                }
                case HorizontalAlignment.Right:
                {
                    if (horizontalAdornerPlacement == AdornerPlacement.Outside)
                    {
                        var adornedWidth = AdornedElement.ActualWidth;
                        return adornedWidth + offsetX;
                    }
                    else
                    {
                        var adornerWidth = this.child.DesiredSize.Width;
                        var adornedWidth = AdornedElement.ActualWidth;
                        var x = adornedWidth - adornerWidth;
                        return x + offsetX;
                    }
                }
                case HorizontalAlignment.Center:
                {
                    var adornerWidth = this.child.DesiredSize.Width;
                    var adornedWidth = AdornedElement.ActualWidth;
                    var x = (adornedWidth / 2) - (adornerWidth / 2);
                    return x + offsetX;
                }
                case HorizontalAlignment.Stretch:
                {
                    return 0.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Determine the Y coordinate of the child.
        /// </summary>
        private double DetermineY()
        {
            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                {
                    if (verticalAdornerPlacement == AdornerPlacement.Outside)
                    {
                        return -child.DesiredSize.Height + offsetY;
                    }
                    else
                    {
                        return offsetY;
                    }
                }
                case VerticalAlignment.Bottom:
                {
                    if (verticalAdornerPlacement == AdornerPlacement.Outside)
                    {
                        var adornedHeight = AdornedElement.ActualHeight;
                        return adornedHeight + offsetY;
                    }
                    else
                    {
                        var adornerHeight = this.child.DesiredSize.Height;
                        var adornedHeight = AdornedElement.ActualHeight;
                        var x = adornedHeight - adornerHeight;
                        return x + offsetY;
                    }
                }
                case VerticalAlignment.Center:
                {
                    var adornerHeight = this.child.DesiredSize.Height;
                    var adornedHeight = AdornedElement.ActualHeight;
                    var x = (adornedHeight / 2) - (adornerHeight / 2);
                    return x + offsetY;
                }
                case VerticalAlignment.Stretch:
                {
                    return 0.0;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Determine the width of the child.
        /// </summary>
        private double DetermineWidth()
        {
            if (!Double.IsNaN(PositionX))
            {
                return this.child.DesiredSize.Width;
            }

            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                {
                    return this.child.DesiredSize.Width;
                }
                case HorizontalAlignment.Right:
                {
                    return this.child.DesiredSize.Width;
                }
                case HorizontalAlignment.Center:
                {
                    return this.child.DesiredSize.Width;
                }
                case HorizontalAlignment.Stretch:
                {
                    return AdornedElement.ActualWidth;
                }
            }

            return 0.0;
        }

        /// <summary>
        /// Determine the height of the child.
        /// </summary>
        private double DetermineHeight()
        {
            if (!Double.IsNaN(PositionY))
            {
                return this.child.DesiredSize.Height;
            }

            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                {
                    return this.child.DesiredSize.Height;
                }
                case VerticalAlignment.Bottom:
                {
                    return this.child.DesiredSize.Height;
                }
                case VerticalAlignment.Center:
                {
                    return this.child.DesiredSize.Height; 
                }
                case VerticalAlignment.Stretch:
                {
                    return AdornedElement.ActualHeight;
                }
            }

            return 0.0;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = PositionX;
            if (Double.IsNaN(x))
            {
                x = DetermineX();
            }
            var y = PositionY;
            if (Double.IsNaN(y))
            {
                y = DetermineY();
            }
            var adornerWidth = DetermineWidth();
            var adornerHeight = DetermineHeight();
            this.child.Arrange(new Rect(x, y, adornerWidth, adornerHeight));
            return finalSize;
        }

        protected override Int32 VisualChildrenCount => 1;

        protected override Visual GetVisualChild(Int32 index)
        {
            return this.child;
        }

        protected override IEnumerator LogicalChildren
        {
            get
            {
                var list = new ArrayList();
                list.Add(this.child);
                return (IEnumerator)list.GetEnumerator();
            }
        }

        /// <summary>
        /// Disconnect the child element from the visual tree so that it may be reused later.
        /// </summary>
        public void DisconnectChild()
        {
            base.RemoveLogicalChild(child);
            base.RemoveVisualChild(child);
        }

        /// <summary>
        /// Override AdornedElement from base class for less type-checking.
        /// </summary>
        public new FrameworkElement AdornedElement => (FrameworkElement)base.AdornedElement;
    }
}