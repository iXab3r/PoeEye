namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using Utilities;

    internal sealed class PoeEyeSettingsViewModel : DisposableReactiveObject
    {
        private bool audioNotificationsEnabled = true;

        private bool clipboardMonitoringEnabled;

        private EditableTuple<string, float>[] currenciesPriceInChaosOrbs = new EditableTuple<string, float>[0];

        private bool isOpen;
        private bool whisperNotificationsEnabled;

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public EditableTuple<string, float>[] CurrenciesPriceInChaosOrbs
        {
            get { return currenciesPriceInChaosOrbs; }
            set { this.RaiseAndSetIfChanged(ref currenciesPriceInChaosOrbs, value); }
        }

        public bool AudioNotificationsEnabled
        {
            get { return audioNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref audioNotificationsEnabled, value); }
        }

        public bool WhisperNotificationsEnabled
        {
            get { return whisperNotificationsEnabled; }
            set { this.RaiseAndSetIfChanged(ref whisperNotificationsEnabled, value); }
        }

        public bool ClipboardMonitoringEnabled
        {
            get { return clipboardMonitoringEnabled; }
            set { this.RaiseAndSetIfChanged(ref clipboardMonitoringEnabled, value); }
        }
    }
}