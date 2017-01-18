﻿using System;
using System.Linq;
using System.Windows.Input;
using Guards;
using PoeChatWheel.Modularity;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeChatWheel.ViewModels
{
    public class PoeChatWheelSettingsViewModel : DisposableReactiveObject, ISettingsViewModel<PoeChatWheelConfig>
    {
        private string hotkey;

        public PoeChatWheelSettingsViewModel()
        {
            HotkeysList = KeyGestureExtensions.GetHotkeyList();
            Hotkey = HotkeysList.First();
        }

        public string Hotkey
        {
            get { return hotkey; }
            set { this.RaiseAndSetIfChanged(ref hotkey, value); }
        }

        public string[] HotkeysList { get; set; }

        public string ModuleName { get; } = "Chat wheel";

        public void Load(PoeChatWheelConfig config)
        {
            Guard.ArgumentNotNull(() => config);

            Hotkey = config.ChatWheelHotkey;
        }

        public PoeChatWheelConfig Save()
        {
            return new PoeChatWheelConfig
            {
                ChatWheelHotkey = Hotkey
            };
        }
    }
}