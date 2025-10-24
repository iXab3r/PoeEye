#nullable enable
using System.Runtime.CompilerServices;

namespace PoeShared.Services;

/// <summary>
/// A pooled, once-only <see cref="IDisposable"/> that invokes a supplied <see cref="Action"/> on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it is:</b> a very low-overhead scope guard for firing a one-shot action when a scope ends. Instances are
/// pooled per thread (LIFO) to make repeated/ nested uses allocation-free on the hot path.
/// </para>
/// <para>
/// <b>Important constraints:</b> because instances are pooled and reused, a reference to a returned object must
/// <b>not</b> be held beyond its lexical scope. If a stale reference is disposed after the instance has been reused for
/// a different action, it will affect the <em>current</em> action. This makes it <b>unsafe as a drop-in replacement</b>
/// for patterns that store disposables for later disposal (e.g., <c>CompositeDisposable</c>, <c>SerialDisposable</c>,
/// fields, etc.). Use this only when disposal is immediate and scoped (e.g., <c>using var _ = DisposableScopedAction.Create(...)</c>).
/// </para>
/// <para>
/// <b>Threading &amp; pooling details:</b> pooling uses a per-thread LIFO stack (capacity 32 by default).
/// Disposal may occur on any thread; returned instances go to that thread's pool. If nesting exceeds capacity, an allocation occurs.
/// </para>
/// </remarks>
public sealed class DisposablePooledAction : IDisposable
{
    private Action? action; // set per rent
    private int armed; // 1=in use, 0=returned

    private DisposablePooledAction()
    {
    }

    private bool IsEmpty => action == null && armed == 0;

    /// <summary>
    /// Disposes the scope and invokes the supplied action exactly once.
    /// </summary>
    /// <remarks>
    /// <para><b>Pros:</b></para>
    /// <list type="bullet">
    ///   <item><description>Exactly-once execution via an atomic state transition (idempotent).</description></item>
    ///   <item><description>Amortized zero allocations on the hot path (instance is returned to a per-thread pool).</description></item>
    ///   <item><description>Thread-safe: multiple concurrent calls to <see cref="Dispose"/> are harmless; only the first triggers the action.</description></item>
    /// </list>
    /// <para><b>Cons / caveats:</b></para>
    /// <list type="bullet">
    ///   <item><description>
    ///   Not safe to hold for later disposal: because the instance is pooled and may be reused for a different action,
    ///   a stale reference disposed later can affect the current action. Do <b>not</b> store this in
    ///   <c>CompositeDisposable</c>, <c>SerialDisposable</c>, fields, or pass it across asynchronous boundaries.
    ///   </description></item>
    ///   <item><description>
    ///   If the action throws, the exception propagates to the caller; the instance is still returned to the pool in a
    ///   <c>finally</c> block.
    ///   </description></item>
    ///   <item><description>
    ///   Disposal may occur on a different thread from creation; the instance will be returned to that thread’s pool.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // ensure exactly once, even with concurrent/stale Dispose calls
        if (Interlocked.Exchange(ref armed, 0) != 1)
        {
            return;
        }

        try
        {
            // snapshot & clear to allow GC and avoid cross-rent bleed
            var a = Interlocked.Exchange(ref action, null);
            a?.Invoke();
        }
        finally
        {
            OnceActionPool.Return(this);
        }
    }

    /// <summary>
    /// Creates a pooled, once-only disposable that will invoke <paramref name="action"/> upon disposal.
    /// </summary>
    /// <param name="action">The callback to run exactly once when <see cref="Dispose"/> is called.</param>
    /// <returns>
    /// A pooled <see cref="DisposablePooledAction"/>. On the hot path, no allocation occurs; otherwise, a new instance
    /// is created and subsequently reused.
    /// </returns>
    /// <remarks>
    /// <para><b>Pros:</b></para>
    /// <list type="bullet">
    ///   <item><description>Very cheap to create in steady state (amortized zero allocations).</description></item>
    ///   <item><description>Ideal for tight, lexical <c>using</c> scopes and nested usage on the same thread.</description></item>
    /// </list>
    /// <para><b>Cons / caveats:</b></para>
    /// <list type="bullet">
    ///   <item><description>
    ///   Not a drop-in replacement for <c>Disposable.Create</c> in Rx when the returned disposable is <b>stored</b>
    ///   and disposed later (e.g., <c>CompositeDisposable</c>). Because instances are reused, stale references can
    ///   interfere with a newer assignment.
    ///   </description></item>
    ///   <item><description>
    ///   Pooling is per-thread with a fixed capacity. If the per-thread stack overflows, a new instance is allocated.
    ///   </description></item>
    ///   <item><description>
    ///   <paramref name="action"/> should ideally be a non-capturing delegate to avoid closure allocations at the call site.
    ///   </description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DisposablePooledAction Create(Action action)
    {
        var s = OnceActionPool.Rent();
        s.Init(action);
        return s;
    }

    // Per-thread LIFO pool — great for nested scopes on the same thread.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Init(Action newAction)
    {
        this.action = newAction;
        Volatile.Write(ref armed, 1); // publish "armed"
    }

    private static class OnceActionPool
    {
        private const int TLS_CAP = 32;

        [ThreadStatic] private static DisposablePooledAction[]? tlsInstanceArray;
        [ThreadStatic] private static int instanceArrayTop;
        private static DisposablePooledAction[] InstanceArray => tlsInstanceArray ??= new DisposablePooledAction[TLS_CAP];

#if DEBUG
        private static long cacheHit;
        private static long cacheMiss;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DisposablePooledAction Rent()
        {
            if (instanceArrayTop > 0)
            {
#if DEBUG
                Interlocked.Increment(ref cacheHit);
#endif
                var s = InstanceArray[--instanceArrayTop];
                InstanceArray[instanceArrayTop] = null!;
                return s!;
            }

#if DEBUG
            Interlocked.Increment(ref cacheMiss);
#endif
            return new DisposablePooledAction();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(DisposablePooledAction s)
        {
            if (!s.IsEmpty)
            {
                throw new ArgumentException("Returned instance must be empty, but was not", nameof(s));
            }
            // at this point instance is expected to be cleared already
            if (instanceArrayTop < TLS_CAP)
            {
                InstanceArray[instanceArrayTop++] = s;
            }
            // overflow: drop on floor
        }
    }
}