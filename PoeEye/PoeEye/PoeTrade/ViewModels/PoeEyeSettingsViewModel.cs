using System;
using System.Linq;
using System.Windows.Input;
using PoeEye.Utilities;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeEyeSettingsViewModel : DisposableReactiveObject
    {
        private bool audioNotificationsEnabled = true;

        private string chatWheelHotkey;

        private bool clipboardMonitoringEnabled;

        private EditableTuple<string, float>[] currenciesPriceInChaosOrbs = new EditableTuple<string, float>[0];

        private bool isOpen;
        private bool whisperNotificationsEnabled;

        public PoeEyeSettingsViewModel()
        {
            var keyGestureConverter = new KeyGestureConverter();
            HotkeysList =
                Enum.GetValues(typeof(Key))
                    .OfType<Key>()
                    .Select(TryToCreateKeyGesture)
                    .Where(x => x != null)
                    .Select(x => x.Key == Key.None ? "None" : keyGestureConverter.ConvertToInvariantString(x))
                    .Distinct()
                    .ToArray();
            ChatWheelHotkey = HotkeysList.First();
        }

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

        public string ChatWheelHotkey
        {
            get { return chatWheelHotkey; }
            set { this.RaiseAndSetIfChanged(ref chatWheelHotkey, value); }
        }

        public string[] HotkeysList { get; set; }

        private KeyGesture TryToCreateKeyGesture(Key key)
        {
            try
            {
                return new KeyGesture(key);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}