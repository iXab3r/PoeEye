using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeShared;
using PoeShared.Modularity;

namespace PoeBud.Models
{
    internal sealed class UserInteractionsManager : IUserInteractionsManager
    {
        private static readonly ILog Log = LogManager.GetLogger<UserInteractionsManager>();

        private readonly IInputSimulator inputSimulator;
        private readonly IUserInputBlocker userInputBlocker;
        private TimeSpan actionDelay;

        public UserInteractionsManager(
            [NotNull] IInputSimulator inputSimulator,
            [NotNull] IConfigProvider<PoeBudConfig> configProvider,
            [NotNull] IUserInputBlocker userInputBlocker)
        {
            Guard.ArgumentNotNull(inputSimulator, nameof(inputSimulator));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(userInputBlocker, nameof(userInputBlocker));

            this.inputSimulator = inputSimulator;
            this.userInputBlocker = userInputBlocker;

            configProvider
                .WhenChanged
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
            Log.Debug($"[UserInteractionsManager] New config, actionDelay: {actionDelay} => {config.UserActionDelay}");
            actionDelay = config.UserActionDelay;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern bool SetCursorPos(uint x, uint y);

            public static void SetCursorPos(Point location)
            {
                SetCursorPos((uint)location.X, (uint)location.Y);
            }
        }
    }
}