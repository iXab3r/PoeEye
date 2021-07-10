using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public class FakeDelayNumberViewModel : DisposableReactiveObject
    {
        private int number;

        public int Number
        {
            get => number;
            set => RaiseAndSetIfChanged(ref number, value);
        }
    }
}