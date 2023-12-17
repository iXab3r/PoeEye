using System.Runtime.CompilerServices;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides a thread-safe flag that can be set atomically.
/// This class is optimized for performance in multi-threaded environments.
/// </summary>
public sealed record AtomicFlag
{
    private int flag;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtomicFlag"/> class.
    /// The flag is initially not set.
    /// </summary>
    public AtomicFlag()
    {
        flag = 0;
    }

    /// <summary>
    /// Gets a value indicating whether the flag is set.
    /// This property is thread-safe.
    /// 
    /// Performance Note:
    /// Uses <see cref="Volatile.Read"/> for a low-overhead atomic read operation.
    /// Marked for aggressive inlining to reduce method call overhead.
    /// </summary>
    public bool IsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref flag) == 1;
    }

    /// <summary>
    /// Sets the flag to true if it is not already set.
    /// This method is thread-safe.
    /// 
    /// Performance Note:
    /// Uses <see cref="Interlocked.Exchange"/> to ensure atomicity of the set operation.
    /// Marked for aggressive inlining to reduce method call overhead.
    /// 
    /// Returns:
    /// True if the flag was set by this call; false if the flag was already set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Set()
    {
        return !IsSet && Interlocked.Exchange(ref flag, 1) == 0;
    }
}