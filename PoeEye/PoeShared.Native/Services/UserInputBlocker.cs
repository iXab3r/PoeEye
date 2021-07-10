using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Services
{
    internal sealed class UserInputBlocker : DisposableReactiveObject, IUserInputBlocker
    {
        private static readonly IFluentLog Log = typeof(UserInputBlocker).PrepareLogger();
        private static readonly object UserInputMonitor = new object();

        private TimeSpan userInputBlockTimeout = TimeSpan.FromSeconds(5);

        public TimeSpan UserInputBlockTimeout
        {
            get => userInputBlockTimeout;
            set => this.RaiseAndSetIfChanged(ref userInputBlockTimeout, value);
        }
        
        public IDisposable Block()
        {
            PerformSynchronizedBlock(true);
            return Disposable.Create(Unblock);
        }
        
        private void Unblock()
        {
            PerformSynchronizedBlock(false);
        }

        private void PerformSynchronizedBlock(bool blockInput)
        {
            if (!Monitor.TryEnter(UserInputMonitor, UserInputBlockTimeout))
            {
                throw new ApplicationException($"Failed to obtain lock on user input to perform operation");
            }

            try
            {
                Log.Info($"Trying to BlockInput({blockInput})");
                if (!BlockInput(blockInput))
                {
                    throw new ApplicationException($"Failed to call BlockInput({blockInput})");
                }
                Log.Info($"BlockInput({blockInput}) succeeded");
            }
            finally
            {
                Monitor.Exit(UserInputMonitor);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool blockInput);
    }
}