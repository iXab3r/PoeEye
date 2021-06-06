using System;
using System.Windows;
using System.Windows.Media;

namespace PoeShared.UI
{
    public sealed class VisualTargetPresentationSource : PresentationSource, IDisposable
    {
        private readonly VisualTarget visualTarget;
        private bool isDisposed;

        public VisualTargetPresentationSource(HostVisual hostVisual)
        {
            visualTarget = new VisualTarget(hostVisual);
            AddSource();
        }

        public Size DesiredSize { get; private set; }

        public override Visual RootVisual
        {
            get { return visualTarget.RootVisual; }
            set
            {
                var oldRoot = visualTarget.RootVisual;

                // Set the root visual of the VisualTarget.  This visual will
                // now be used to visually compose the scene.
                visualTarget.RootVisual = value;

                // Tell the PresentationSource that the root visual has
                // changed.  This kicks off a bunch of stuff like the
                // Loaded event.
                RootChanged(oldRoot, value);

                // Kickoff layout...
                if (value is UIElement rootElement)
                {
                    rootElement.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    rootElement.Arrange(new Rect(rootElement.DesiredSize));

                    DesiredSize = rootElement.DesiredSize;
                }
                else
                    DesiredSize = new Size(0, 0);
            }
        }

        protected override CompositionTarget GetCompositionTargetCore()
        {
            return visualTarget;
        }

        public override bool IsDisposed => isDisposed;

        public void Dispose()
        {
            RemoveSource();
            isDisposed = true;
        }
    }
}
