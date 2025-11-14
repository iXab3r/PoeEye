using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoeShared.Scaffolding
{
    /// <summary>
    /// Provides helpers for running parallel work with a <b>process-wide</b> concurrency limit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This utility was introduced to avoid the common failure mode of spawning independent
    /// <see cref="Parallel.ForEach{TSource}"/> loops all over the codebase, each with their own
    /// <c>MaxDegreeOfParallelism</c>, which together can easily oversubscribe the machine and make
    /// the OS appear hung.
    /// </para>
    /// <para>
    /// All parallel loops executed via this class share a single
    /// <see cref="TaskScheduler"/> (based on <see cref="ConcurrentExclusiveSchedulerPair"/>)
    /// that caps global concurrency to <see cref="Environment.ProcessorCount"/>.
    /// This means multiple call sites cooperate instead of competing, and the total number of
    /// concurrently executing handlers is bounded.
    /// </para>
    /// <para>
    /// The API also exposes <paramref name="minDegreeOfParallelism"/> to avoid paying the
    /// overhead of parallel scheduling for tiny collections. When the number of items is below
    /// that threshold, items are processed sequentially in the calling thread.
    /// </para>
    /// <para>
    /// <b>Caution:</b> if a <paramref name="handler"/> invoked via this utility calls back
    /// into <see cref="ParallelUtils"/> (directly or indirectly), that creates a form
    /// of nested parallelism and can reduce throughput or, in pathological cases, cause
    /// scheduler starvation. The intended usage is that <c>ParallelUtils</c> is the
    /// <em>outermost</em> parallelization layer for CPU-bound work.
    /// </para>
    /// </remarks>
    public static class ParallelUtils
    {
        /// <summary>
        /// Default maximum degree of parallelism used by <see cref="ForEach{T}(IReadOnlyList{T},Action{T},int)"/>
        /// and <see cref="ForEach{T}(ConcurrentQueue{T},Action{T},int)"/> when no explicit maximum is provided.
        /// </summary>
        /// <remarks>
        /// This is intentionally conservative (<c>ProcessorCount / 4</c>) to avoid flooding the shared
        /// scheduler with too many CPU-bound tasks from a single call site.
        /// The value is further clamped against <see cref="Environment.ProcessorCount"/> on each call.
        /// </remarks>
        public static readonly int DefaultMaxDegreeOfParallelism =
            Math.Max(1, Environment.ProcessorCount / 4);

        /// <summary>
        /// Process-wide concurrency limiter used by all parallel operations in this class.
        /// </summary>
        /// <remarks>
        /// The <see cref="ConcurrentExclusiveSchedulerPair"/> is configured with
        /// <see cref="TaskScheduler.Default"/> as the underlying scheduler and
        /// <see cref="Environment.ProcessorCount"/> as the maximum concurrency.
        /// We always use the <see cref="ConcurrentExclusiveSchedulerPair.ConcurrentScheduler"/>
        /// so all parallel loops cooperate and share a single global concurrency budget.
        /// </remarks>
        private static readonly ConcurrentExclusiveSchedulerPair SchedulerPair =
            new(TaskScheduler.Default, Environment.ProcessorCount);

        /// <summary>
        /// Shared scheduler used by all <see cref="Parallel.ForEach{TSource}"/> calls in this class.
        /// </summary>
        private static readonly TaskScheduler GlobalScheduler = SchedulerPair.ConcurrentScheduler;

        static ParallelUtils()
        {
        }

        /// <summary>
        /// Dequeues all available items from a <see cref="ConcurrentQueue{T}"/> and processes them in parallel,
        /// using a shared global scheduler to limit total concurrency across the process.
        /// </summary>
        /// <typeparam name="T">Type of items to process.</typeparam>
        /// <param name="queue">
        /// The source queue to drain. All currently enqueued items will be removed and processed.
        /// </param>
        /// <param name="handler">The action to execute for each dequeued item.</param>
        /// <param name="minDegreeOfParallelism">
        /// Minimum number of items required to justify parallel processing. If the number of
        /// dequeued items is below this threshold, they are processed sequentially in the calling thread.
        /// </param>
        /// <remarks>
        /// This overload uses <see cref="DefaultMaxDegreeOfParallelism"/> as an upper bound for
        /// per-call parallelism. The actual concurrency is also capped by the global scheduler.
        /// </remarks>
        public static void ForEach<T>(
            ConcurrentQueue<T> queue,
            Action<T> handler,
            int minDegreeOfParallelism = 1)
        {
            ForEach(queue, handler, minDegreeOfParallelism, DefaultMaxDegreeOfParallelism);
        }

        /// <summary>
        /// Processes an <see cref="IReadOnlyList{T}"/> in parallel using a shared global scheduler,
        /// with a conservative default upper bound on per-call parallelism.
        /// </summary>
        /// <typeparam name="T">Type of items to process.</typeparam>
        /// <param name="items">The items to process.</param>
        /// <param name="handler">The action to execute for each item.</param>
        /// <param name="minDegreeOfParallelism">
        /// Minimum number of items required to justify parallel processing. If the number of
        /// items is below this threshold, they are processed sequentially in the calling thread.
        /// </param>
        public static void ForEach<T>(
            IReadOnlyList<T> items,
            Action<T> handler,
            int minDegreeOfParallelism = 1)
        {
            ForEach(items, handler, minDegreeOfParallelism, DefaultMaxDegreeOfParallelism);
        }

        /// <summary>
        /// Processes an <see cref="IReadOnlyList{T}"/> in parallel using a shared global scheduler,
        /// with explicit control over minimum and maximum per-call parallelism.
        /// </summary>
        /// <typeparam name="T">Type of items to process.</typeparam>
        /// <param name="items">The items to process.</param>
        /// <param name="handler">The action to execute for each item.</param>
        /// <param name="minDegreeOfParallelism">
        /// Minimum number of items required to justify parallel processing. If the number of
        /// items is below this threshold, they are processed sequentially in the calling thread.
        /// </param>
        /// <param name="maxDegreeOfParallelism">
        /// Upper bound on the number of concurrent handlers for this call. The effective concurrency
        /// may be lower, as it is also capped by <see cref="Environment.ProcessorCount"/> and the
        /// global scheduler.
        /// </param>
        /// <remarks>
        /// This method is intended for CPU-bound work. For I/O-bound work, consider using async
        /// patterns instead of <see cref="Parallel.ForEach{TSource}"/>.
        /// </remarks>
        public static void ForEach<T>(
            IReadOnlyList<T> items,
            Action<T> handler,
            int minDegreeOfParallelism,
            int maxDegreeOfParallelism)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (items.Count == 0)
            {
                return;
            }

            if (items.Count >= minDegreeOfParallelism)
            {
                // Clamp to a sensible per-call limit; the global scheduler still enforces a process-wide cap.
                maxDegreeOfParallelism = Math.Max(1, Math.Min(maxDegreeOfParallelism, Environment.ProcessorCount));

                Parallel.ForEach(
                    items,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism,
                        TaskScheduler = GlobalScheduler
                    },
                    handler);
            }
            else
            {
                foreach (var item in items)
                {
                    handler(item);
                }
            }
        }

        /// <summary>
        /// Dequeues all available items from a <see cref="ConcurrentQueue{T}"/> and processes them in parallel,
        /// using a shared global scheduler and explicit per-call parallelism limits.
        /// </summary>
        /// <typeparam name="T">Type of items to process.</typeparam>
        /// <param name="queue">
        /// The source queue to drain. All currently enqueued items will be removed and processed.
        /// </param>
        /// <param name="handler">The action to execute for each dequeued item.</param>
        /// <param name="minDegreeOfParallelism">
        /// Minimum number of dequeued items required to justify parallel processing. If the number of
        /// dequeued items is below this threshold, they are processed sequentially in the calling thread.
        /// </param>
        /// <param name="maxDegreeOfParallelism">
        /// Upper bound on the number of concurrent handlers for this call. The effective concurrency
        /// is further limited by <see cref="Environment.ProcessorCount"/> and the global scheduler.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method first takes a snapshot of the queue contents and drains them into a local list
        /// under a lock. This ensures each queued item is processed at most once by this call, but it
        /// also means producers are blocked while the snapshot is taken.
        /// </para>
        /// <para>
        /// Subsequent items enqueued after the snapshot will be processed by later calls to
        /// <see cref="ForEach{T}(ConcurrentQueue{T},Action{T},int,int)"/>.
        /// </para>
        /// </remarks>
        public static void ForEach<T>(
            ConcurrentQueue<T> queue,
            Action<T> handler,
            int minDegreeOfParallelism,
            int maxDegreeOfParallelism)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var items = new List<T>();

            // Snapshot and drain all currently queued items under a lock
            lock (queue)
            {
                while (queue.TryDequeue(out var item))
                {
                    items.Add(item);
                }
            }

            ForEach(items, handler, minDegreeOfParallelism, maxDegreeOfParallelism);
        }
    }
}