namespace PoeShared.Scaffolding
{
    public abstract class CloseableReactiveObject : DisposableReactiveObject, ICloseable
    {
        private ICloseController closeController;

        public ICloseController CloseController
        {
            get => closeController;
            set => RaiseAndSetIfChanged(ref closeController, value);
        }
    }
}