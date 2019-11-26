using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using log4net;

namespace PoeShared.Scaffolding
{
    public sealed class BenchmarkTimer : IDisposable
    {
        private static readonly ILog DefaultLogger = LogManager.GetLogger(typeof(BenchmarkTimer));

        private readonly string benchmarkName;
        private readonly ILog logger;
        private readonly string propertyName;
        private readonly ConcurrentQueue<string> operations = new ConcurrentQueue<string>();
        private TimeSpan previousOperationTimestamp;
        private readonly Stopwatch sw;

        public BenchmarkTimer(string benchmarkName, ILog logger = null, [CallerMemberName] string propertyName = null)
        {
            this.benchmarkName = benchmarkName;
            this.logger = logger ?? DefaultLogger;
            this.propertyName = propertyName ?? "unknown";
            sw = Stopwatch.StartNew();
        }

        public void ResetStep()
        {
            previousOperationTimestamp = sw.Elapsed;
        }

        public void Step(string message)
        {
            var timestamp = sw.Elapsed;
            AddStep(message);
            previousOperationTimestamp = timestamp;
        }

        private void AddStep(string message)
        {
            operations.Enqueue($"[{propertyName}] [{(sw.Elapsed - previousOperationTimestamp).TotalMilliseconds:F0}ms] {message}");
        }
        
        public void Dispose()
        {
            sw.Stop();
            if (logger.IsInfoEnabled)
            {
                logger.Info($"[{propertyName}] [{sw.Elapsed.TotalMilliseconds:F0}ms] {benchmarkName}{(operations.Count <= 0 ? string.Empty : $"\n\t{string.Join("\n\t", operations)}")}");
            }
        }
    }
}