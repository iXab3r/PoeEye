using PoeShared.Scaffolding;

namespace PoeShared.UI.Bindings
{
    public class StubViewModel : BindableReactiveObject
    {
        private double doubleProperty;
        private int intProperty;

        private string stringProperty;

        public int IntProperty
        {
            get => intProperty;
            set => RaiseAndSetIfChanged(ref intProperty, value);
        }

        public string StringProperty
        {
            get => stringProperty;
            set => RaiseAndSetIfChanged(ref stringProperty, value);
        }

        public double DoubleProperty
        {
            get => doubleProperty;
            set => RaiseAndSetIfChanged(ref doubleProperty, value);
        }
    }
}