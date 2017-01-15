using System;

namespace PoeBud.Models
{
    using System.Windows;

    using WindowsInput.Native;

    internal interface IUserInteractionsManager
    {
        void MoveMouseTo(Point location);

        void SendControlLeftClick();

        void SendClick();

        void SendKey(VirtualKeyCode keyCode);

        void Delay(TimeSpan delay);

        void Delay();
    }
}