
using BenchmarkDotNet.Order;
using PoeShared.Tests;

namespace PoeShared.Benchmarks;

[TestFixture]
[Category("Benchmark")]
public class BenchmarkBase : FixtureBase
{
    protected IConfig Config => PrepareDefaultConfig();

    public static Job PrepareJob(string targetFrameworkMoniker)
    {
        var job = Job.ShortRun.UnfreezeCopy();
        job = job
            .WithToolchain(
                CsProjCoreToolchain.From(
                    new NetCoreAppSettings(
                        targetFrameworkMoniker: targetFrameworkMoniker,
                        runtimeFrameworkVersion: null,
                        name: "7.0").WithTimeout(TimeSpan.FromMinutes(10))))
            .WithArguments(new Argument[]{ new MsBuildArgument("/p:ValidateExecutableReferencesMatchSelfContained=false") })
            .AsDefault();
        return job;
    }

    protected void RunBenchmark(string methodName)
    {
        var config = PrepareConfig();

        var testType = GetType();
        var benchmarkMethod = this.GetType().GetMethod(methodName);
        BenchmarkRunner.Run(type: testType, methods: new[] { benchmarkMethod }, config);
    }
        
    protected virtual ManualConfig PrepareConfig()
    {
        var summaryStyle = DefaultConfig.Instance
            .SummaryStyle
            .WithMaxParameterColumnWidth(40);

        var result = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithOption(ConfigOptions.JoinSummary, false)
            .WithSummaryStyle(summaryStyle)
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method));
        
        return result;
    }

    protected IConfig PrepareDefaultConfig(string targetFrameworkMoniker = "net7-windows10.0.20348.0")
    {
        var job = PrepareJob(targetFrameworkMoniker);
        var result = PrepareConfig()
            .AddJob(job);
        return result;
    }
}