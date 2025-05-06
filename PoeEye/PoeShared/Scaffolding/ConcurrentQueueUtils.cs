namespace PoeShared.Scaffolding;

public static class ConcurrentQueueUtils
{
    public static readonly int MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount);

    static ConcurrentQueueUtils()
    {
    }

    public static void Process<T>(
        ConcurrentQueue<T> unprocessedAssemblies,
        Action<T> handler,
        int minDegreeOfParallelism = 2)
    {
        Process(unprocessedAssemblies, handler, MaxDegreeOfParallelism, minDegreeOfParallelism);
    }

    public static void Process<T>(
        ConcurrentQueue<T> unprocessedAssemblies,
        Action<T> handler,
        int maxDegreeOfParallelism,
        int minDegreeOfParallelism)
    {
        var assembliesToProcess = new List<T>();
        lock (unprocessedAssemblies)
        {
            while (unprocessedAssemblies.TryDequeue(out var assembly))
            {
                assembliesToProcess.Add(assembly);
            }
        }

        if (assembliesToProcess.Count >= minDegreeOfParallelism)
        {
            Parallel.ForEach(
                assembliesToProcess,
                new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism },
                handler);
        }
        else
        {
            foreach (var assembly in assembliesToProcess)
            {
                handler(assembly);
            }
        }
    }
}