using System.Threading;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace PoeShared.Benchmarks.Scaffolding;

[TestFixture]
[MemoryDiagnoser]
public class SleepPerformanceFixture : BenchmarkBase
{
    [Test]
    public void RunBenchmarks()
    {
        BenchmarkRunner.Run<SleepPerformanceFixture>(Config);
    }

    [Test]
    [Benchmark]
    [TestCaseSource(nameof(ShouldSleepCases))]
    [ArgumentsSource(nameof(ShouldSleepCases))]
    public void ShouldSleepToken(ShouldSleepArgs args)
    {
        //Given
        //When
        for (int i = 0; i < args.Count; i++)
        {
            //Thread.Sleep(0);
            args.CancellationToken.Sleep(args.Timeout, null);
        }
    }

    [Test]
    [Benchmark]
    [TestCaseSource(nameof(ShouldSleepCases))]
    [ArgumentsSource(nameof(ShouldSleepCases))]
    public void ShouldSleep(ShouldSleepArgs args)
    {
        //Given
        //When
        for (int i = 0; i < args.Count; i++)
        {
            TaskExtensions.Sleep(args.Timeout, null);
        }
    }
    
    public readonly record struct ShouldSleepArgs(int Timeout, int Count)
    {
        public CancellationToken CancellationToken { get; } = CancellationToken.None;

        public override string ToString()
        {
            var toStringBuilder = new ToStringBuilder("Args");
            toStringBuilder.AppendParameter(nameof(Timeout), Timeout);
            toStringBuilder.AppendParameter(nameof(Count), Count);
            return toStringBuilder.ToString();
        }
    }
    
    public static IEnumerable<ShouldSleepArgs> ShouldSleepCases()
    {
        yield return new ShouldSleepArgs(0, 0);
        yield return new ShouldSleepArgs(0, 1);
        yield return new ShouldSleepArgs(0, 2);
        yield return new ShouldSleepArgs(1, 1);
      /*  yield return new ShouldSleepArgs(TaskExtensions.SleepLowPrecisionThresholdMs, 1);
        yield return new ShouldSleepArgs(TaskExtensions.SleepLowPrecisionThresholdMs + 1, 1);
        yield return new ShouldSleepArgs(TaskExtensions.SleepLowPrecisionThresholdMs + 2, 1);
        yield return new ShouldSleepArgs(TaskExtensions.SleepLowPrecisionThresholdMs + 3, 1);
        yield return new ShouldSleepArgs(1, 1000);*/
    }
}