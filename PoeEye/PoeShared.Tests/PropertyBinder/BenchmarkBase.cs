using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using NUnit.Framework;

namespace PoeShared.Tests.PropertyBinder
{
    [TestFixture]
    public class BenchmarkBase
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
                            name: "5.0")))
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
        
        protected virtual IConfig PrepareConfig()
        {
            var summaryStyle = DefaultConfig.Instance.SummaryStyle
                .WithMaxParameterColumnWidth(40);
            
            var result = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .WithOption(ConfigOptions.JoinSummary, false)
                .WithSummaryStyle(summaryStyle);
            
            return result;
        }

        protected IConfig PrepareDefaultConfig(string targetFrameworkMoniker =  "net5.0-windows7")
        {
            var job = PrepareJob(targetFrameworkMoniker);
            var result = PrepareConfig()
                .AddJob(job);
            return result;
        }
    }
}