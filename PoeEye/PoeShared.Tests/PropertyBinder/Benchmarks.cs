using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using NUnit.Framework;

namespace PoeShared.Tests.PropertyBinder
{
    [TestFixture]
    public class Benchmarks
    {
        private static readonly IConfig Config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .AddJob(
                Job.Default.WithToolchain(
                        CsProjCoreToolchain.From(
                            new NetCoreAppSettings(
                                targetFrameworkMoniker: "net5.0-windows7", // the key to make it work
                                runtimeFrameworkVersion: null,
                                name: "5.0")))
                    .AsDefault());

        [Test]
        public void RunCollectionsBenchmarks()
        {
            BenchmarkRunner.Run<BinderPerformanceCollectionsFixture>(Config);
        }
        
        [Test]
        public void RunModelBenchmarks()
        {
            BenchmarkRunner.Run<BinderPerformanceModelFixture>(Config);
        }
    }
}