using System;
using System.Windows;
using WindowsInput.Native;

namespace PoeBud.Models
{
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