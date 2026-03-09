using System.Threading;
using NUnit.Framework;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Fody.Tests;

public abstract class FixtureBase
{
    public IFluentLog Log { get; private set; }

    private static long GlobalRunIdx = 0;
    private string runIdx;

    protected FixtureBase()
    {
        Log = GetType().PrepareLogger().WithSuffix(() => runIdx);
    }

    [OneTimeSetUp]
    public void OneTimeSetUpTest()
    {
        Log.Debug("OneTimeSetUp starting");
        OneTimeSetUp();
        Log.Debug("OneTimeSetUp completed");
    }

    [SetUp]
    public void SetUpTest()
    {
        runIdx= $"RunIdx: {Interlocked.Increment(ref GlobalRunIdx)}"; 
        SetUp();
    }

    [TearDown]
    public void TearDownTest()
    {
        Log.Debug("Tearing down test");
        TearDown();
        Log.Debug("Tear down completed");
    }

    protected virtual void SetUp(){}
    
    protected virtual void TearDown(){}
    protected virtual void OneTimeSetUp(){}
}