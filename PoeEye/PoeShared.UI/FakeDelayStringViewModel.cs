using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public class FakeDelayStringViewModel : DisposableReactiveObject
    {
        private string name;

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }
    }
}