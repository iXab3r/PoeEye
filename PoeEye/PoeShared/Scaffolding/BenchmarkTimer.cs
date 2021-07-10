using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding
{
    public sealed class BenchmarkTimer : IDisposable
    {
        private static readonly IFluentLog DefaultLogger = LogManager.GetLogger(typeof(BenchmarkTimer)).ToFluent();

        private readonly string benchmarkName;
        private readonly IFluentLog logger;
        private readonly string propertyName;
        private readonly ConcurrentQueue<string> operations = new ConcurrentQueue<string>();
        private TimeSpan previousOperationTimestamp;
        private readonly Stopwatch sw;
        private TimeSpan loggingElapsedThreshold = TimeSpan.Zero;
        private Func<bool> predicate = () => true;
        private bool logEachStep = true;
        private bool logOnDisposal = false;

        public BenchmarkTimer(string benchmarkName, ILog logger, [CallerMemberName] string propertyName = null) : this(benchmarkName, logger?.ToFluent(), propertyName)
        {
        }
        public BenchmarkTimer(string benchmarkName, IFluentLog logger = null, [CallerMemberName] string propertyName = null)
        {
            this.benchmarkName = benchmarkName;
            this.logger = logger ?? DefaultLogger;
            this.propertyName = propertyName ?? "unknown";
            sw = Stopwatch.StartNew();
            Step($"=> {benchmarkName}");
        }
        
        public TimeSpan Elapsed => sw.Elapsed;

        public BenchmarkTimer ResetStep()
        {
            previousOperationTimestamp = sw.Elapsed;
            return this;
        }

        public BenchmarkTimer WithoutLoggingEachStep()
        {
            logEachStep = false;
            return this;
        }

        public BenchmarkTimer WithLoggingEachStep()
        {
            logEachStep = true;
            return this;
        }
        
        public BenchmarkTimer WithoutLoggingOnDisposal()
        {
            logOnDisposal = false;
            return this;
        }

        public BenchmarkTimer WithLoggingOnDisposal()
        {
            logOnDisposal = true;
            return this;
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

        public BenchmarkTimer Step(Func<string> messageFactory)
        {
            AddStep(messageFactory);
            return this;
        }
        
        public BenchmarkTimer Step(string message)
        {
            return Step(() => message);
        }
        
        private void AddStep(Func<string> messageFactory)
        {
            if (predicate != null && !predicate())
            {
                return;
            }
            var timestamp = sw.Elapsed;

            var logMessage = $"[{propertyName}] [{(sw.Elapsed - previousOperationTimestamp).TotalMilliseconds:F1}ms] {messageFactory()}";
            operations.Enqueue(logMessage);
            if (logEachStep && logger.IsDebugEnabled)
            {
                logger.Debug(FormattableStringFactory.Create(logMessage));
            }
            previousOperationTimestamp = timestamp;
        }
        
        public void Dispose()
        {
            sw.Stop();
            if (logOnDisposal && logger.IsDebugEnabled && sw.Elapsed > loggingElapsedThreshold)
            {
                logger.Debug($"[{propertyName}] [{sw.Elapsed.TotalMilliseconds:F1}ms] <= {benchmarkName}{(operations.Count <= 0 ? string.Empty : $"\n\t{string.Join("\n\t", operations)}")}");
            }
        }
    }
}