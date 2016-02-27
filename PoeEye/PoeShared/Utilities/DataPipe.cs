namespace PoeShared.Utilities
{
    using System.Windows;

    public class DataPipe : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new DataPipe();
        }

        #region Source (DependencyProperty)

        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source",
                typeof (object),
                typeof (DataPipe),
                new FrameworkPropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DataPipe) d).OnSourceChanged(e);
        }

        protected virtual void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            Target = e.NewValue;
        }

        #endregion

        #region Target (DependencyProperty)

        public object Target
        {
            get { return GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(
                "Target",
                typeof (object),
                typeof (DataPipe),
                new FrameworkPropertyMetadata(null));

        #endregion
    }
}