using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PoeShared.Scaffolding
{
    public sealed class OperationTimer : IDisposable
    {
        private readonly Stopwatch sw;

        private readonly Action<TimeSpan> endAction;

        private TimeSpan previousOperationTimestamp;

        public OperationTimer(Action<TimeSpan> endAction)
        {
            this.endAction = endAction;
            sw = Stopwatch.StartNew();
        }

        public void PutTimestamp()
        {
            previousOperationTimestamp = sw.Elapsed;
        }

        public void LogOperation(Action<TimeSpan> action)
        {
            var timestamp = sw.Elapsed;
            action(timestamp - previousOperationTimestamp);
            previousOperationTimestamp = timestamp;
        }

        public TimeSpan Elapsed => sw.Elapsed;

        public void Dispose()
        {
            sw.Stop();
            endAction(sw.Elapsed);
        }
    }
}