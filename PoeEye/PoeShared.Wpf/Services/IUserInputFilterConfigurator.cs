using System;
using PoeShared.UI;

namespace PoeShared.Wpf.Services
{
    public interface IUserInputFilterConfigurator
    {
        IDisposable AddToWhitelist(HotkeyGesture hotkey);

        bool IsInWhitelist(HotkeyGesture hotkey);
    }
}