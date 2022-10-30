using System.Reactive.Concurrency;
using Castle.DynamicProxy;

namespace PoeShared.Caching;

internal sealed class CachingInterceptor : IInterceptor
{
    private readonly IClock clock;
    private readonly ConcurrentDictionary<InvocationKey, InvocationResult> cache = new();

    public CachingInterceptor(IFluentLog log, IClock clock)
    {
        this.clock = clock;
        Log = log;
    }

    public IFluentLog Log { get; }
    
    /// <summary>
    ///   Items is considered valid if it's lifetime is less than TTL, will be replaced with a new one otherwise on next request
    /// </summary>
    public TimeSpan TimeToLive { get; set; }

    public void Intercept(IInvocation invocation)
    {
        var key = new InvocationKey
        {
            TypeName = invocation.Method.DeclaringType?.Name,
            MethodName = invocation.Method.Name,
            Arguments = invocation.Arguments.Select(x => $"{x ?? "null"}").JoinStrings(", ")
        };
        var log = Log.WithSuffix($"{key.TypeName}.{key.MethodName}({(!string.IsNullOrEmpty(key.Arguments) ? key.Arguments : string.Empty)})");
        log.Debug(() => $"Invocation requested from cache via key {key}");

        InvocationResult AddValueFactory(InvocationKey key)
        {
            log.Debug(() => $"Performing invocation on source");
            try
            {
                invocation.Proceed();
            }
            catch (Exception e)
            {
                Log.Error("Invocation has thrown exception", e);
                throw;
            }
            log.Debug(() => $"Invocation on source completed: {invocation.ReturnValue}");
            return new InvocationResult {Timestamp = clock.UtcNow, ReturnValue = invocation.ReturnValue};
        }

        var result = cache.AddOrUpdate(key, AddValueFactory, (key, existing) =>
        {
            var now = clock.UtcNow;
            var elapsed = now - existing.Timestamp;
            return elapsed > TimeToLive ? AddValueFactory(key) : existing;
        });
        
        log.Debug(() => $"Cache result: {result}");
        invocation.ReturnValue = result.ReturnValue;
        log.Debug(() => $"Returning result(elapsed: {(clock.UtcNow - result.Timestamp).TotalMilliseconds}ms): {invocation.ReturnValue}");
    }

    private record struct InvocationKey
    {
        public string TypeName { get; set; }
        public string MethodName { get; set; }
        public string Arguments { get; set; }
    }

    private record struct InvocationResult
    {
        public DateTime Timestamp { get; set; }
        public object ReturnValue { get; set; }
    }
}