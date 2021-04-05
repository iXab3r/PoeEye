using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
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
        private TimeSpan loggingElapsedThreshold = TimeSpan.Zero;
        private Func<bool> predicate = () => true;

        public BenchmarkTimer(string benchmarkName, ILog logger = null, [CallerMemberName] string propertyName = null)
        {
            this.benchmarkName = benchmarkName;
            this.logger = logger ?? DefaultLogger;
            this.propertyName = propertyName ?? "unknown";
            sw = Stopwatch.StartNew();
        }
        
        public TimeSpan Elapsed => sw.Elapsed;

        public void ResetStep()
        {
            previousOperationTimestamp = sw.Elapsed;
        }
        
        public BenchmarkTimer WithCondition([NotNull] Func<bool> predicate)
        {
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this;
        }

        public BenchmarkTimer WithMinElapsedThreshold(TimeSpan minElapsedThreshold)
        {
            loggingElapsedThreshold = minElapsedThreshold;
            return this;
        }

        public void Step(string message)
        {
            var timestamp = sw.Elapsed;
            AddStep(message);
            previousOperationTimestamp = timestamp;
        }
        
        public void StepIf(string message, TimeSpan elapsedThreshold)
        {
            if (sw.Elapsed < elapsedThreshold)
            {
                return;
            }

            Step(message);
        }

        private void AddStep(string message)
        {
            if (predicate != null && !predicate())
            {
                return;
            }
            var logMessage = $"[{propertyName}] [{(sw.Elapsed - previousOperationTimestamp).TotalMilliseconds:F1}ms] {message}";
            operations.Enqueue(logMessage);
            if (logger.IsDebugEnabled)
            {
                logger.Debug(logMessage);
            }
        }
        
        public void Dispose()
        {
            sw.Stop();
            if (logger.IsDebugEnabled && !operations.IsEmpty && sw.Elapsed > loggingElapsedThreshold)
            {
                logger.Debug($"[{propertyName}] [{sw.Elapsed.TotalMilliseconds:F1}ms] {benchmarkName}{(operations.Count <= 0 ? string.Empty : $"\n\t{string.Join("\n\t", operations)}")}");
            }
        }
    }
}