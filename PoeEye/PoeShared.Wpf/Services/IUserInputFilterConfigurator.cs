using System;
using PoeShared.UI;

namespace PoeShared.Services
{
    public interface IUserInputFilterConfigurator
    {
        IDisposable AddToWhitelist(HotkeyGesture hotkey);
    }
}