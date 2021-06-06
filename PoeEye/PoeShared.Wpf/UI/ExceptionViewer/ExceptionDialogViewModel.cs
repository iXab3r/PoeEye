using System;
using System.Windows.Input;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI
{
    internal sealed class ExceptionDialogViewModel : DisposableReactiveObject
    {
        private readonly ICloseController closeController;
        private ExceptionDialogConfig config;
        private Exception exceptionSource;

        public ExceptionDialogViewModel(ICloseController closeController)
        {
            this.closeController = closeController;
            
            this.RaiseWhenSourceValue(x => x.Title, this, x => x.Config).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.AppName, this, x => x.AppName).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.ExceptionText, this, x => x.ExceptionText).AddTo(Anchors);
            CloseCommand = CommandWrapper.Create(closeController.Close);
        }

        public ExceptionDialogConfig Config
        {
            get => config;
            set => this.RaiseAndSetIfChanged(ref config, value);
        }

        public Exception ExceptionSource
        {
            get => exceptionSource;
            set => this.RaiseAndSetIfChanged(ref exceptionSource, value);
        }

        public string ExceptionText => exceptionSource?.ToString();

        public string Title => config.Title;

        public string AppName => config.AppName;
        
        public ICommand CloseCommand { get; }
    }
}