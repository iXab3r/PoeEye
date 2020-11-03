using System.Windows;

namespace PoeShared.Scaffolding
{
    public class DataPiping
    {
        public static readonly DependencyProperty DataPipesProperty =
            DependencyProperty.RegisterAttached(
                "DataPipes",
                typeof(DataPipeCollection),
                typeof(DataPiping),
                new UIPropertyMetadata(null));

        public static void SetDataPipes(DependencyObject o, DataPipeCollection value)
        {
            o.SetValue(DataPipesProperty, value);
        }

        public static DataPipeCollection GetDataPipes(DependencyObject o)
        {
            return (DataPipeCollection) o.GetValue(DataPipesProperty);
        }
    }
}