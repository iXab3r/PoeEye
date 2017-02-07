using PoeShared;
using PoeShared.Modularity;
using ReactiveUI;

namespace PoeBud.Models
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;

    using WindowsInput;
    using WindowsInput.Native;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    internal sealed class UserInteractionsManager : IUserInteractionsManager
    {
        private TimeSpan actionDelay;

        private readonly IInputSimulator inputSimulator;
        private readonly IUserInputBlocker userInputBlocker;

        public UserInteractionsManager(
                [NotNull] IInputSimulator inputSimulator,
                [NotNull] IConfigProvider<PoeBudConfig> configProvider,
                [NotNull] IUserInputBlocker userInputBlocker)
        {
            Guard.ArgumentNotNull(() => inputSimulator);
            Guard.ArgumentNotNull(() => configProvider);
            Guard.ArgumentNotNull(() => userInputBlocker);

            this.inputSimulator = inputSimulator;
            this.userInputBlocker = userInputBlocker;

            configProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Subscribe(ReloadConfig);
        }

        public void SendControlLeftClick()
        {
            using (userInputBlocker.Block())
            {
                Delay();
                inputSimulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                Delay();
                inputSimulator.Mouse.LeftButtonClick();
                Delay();
                inputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
            }
        }

        public void SendClick()
        {
            using (userInputBlocker.Block())
            {
                Delay();
                inputSimulator.Mouse.LeftButtonClick();
            }
        }

        public void SendKey(VirtualKeyCode keyCode)
        {
            inputSimulator.Keyboard.KeyPress(keyCode);
        }

        public void MoveMouseTo(Point location)
        {
            Delay();
            NativeMethods.SetCursorPos(location);
        }

        public void Delay(TimeSpan delay)
        {
            Thread.Sleep(delay);
        }

        public void Delay()
        {
            Delay(actionDelay);
        }

        private void ReloadConfig(IPoeBudConfig config)
        {
            Log.Instance.Debug($"[UserInteractionsManager] New config, actionDelay: {actionDelay} => {config.UserActionDelay}");
            actionDelay = config.UserActionDelay;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            static extern bool SetCursorPos(uint x, uint y);

            public static void SetCursorPos(Point location)
            {
                SetCursorPos((uint)location.X, (uint)location.Y);
            }
        }
    }
}