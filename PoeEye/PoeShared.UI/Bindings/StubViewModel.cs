using PoeShared.Bindings;

namespace PoeShared.UI.Bindings
{
    public class StubViewModel : BindableReactiveObject
    {

        public int IntProperty { get; set; }

        public string StringProperty { get; set; }

        public double DoubleProperty { get; set; }
    }
}