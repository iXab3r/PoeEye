using System;
using System.Diagnostics;

namespace PoeShared.Scaffolding
{
    public sealed class OperationTimer : IDisposable
    {
        private readonly Stopwatch sw;

        private readonly Action<TimeSpan> endAction;

        public OperationTimer(Action<TimeSpan> endAction)
        {
            this.endAction = endAction;
            sw = Stopwatch.StartNew();
        }

        public TimeSpan Elapsed => sw.Elapsed;

        public void Dispose()
        {
            sw.Stop();
            endAction(sw.Elapsed);
        }
    }
}