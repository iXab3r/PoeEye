﻿using System.Reactive.Concurrency;
using System.Threading;
using NUnit.Framework;
using AutoFixture;
using AutoFixture.AutoMoq;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Tests;

public abstract class FixtureBase
{
    public Fixture Container { get; private set; }
    public IFluentLog Log { get; private set; }

    private static long GlobalRunIdx = 0;
    private string runIdx;

    protected FixtureBase()
    {
        Log = GetType().PrepareLogger().WithSuffix(() => runIdx);
    }

    [SetUp]
    public void SetUpTest()
    {
        runIdx= $"RunIdx: {Interlocked.Increment(ref GlobalRunIdx)}"; 
        
        Container = new Fixture();
        Container.Customize(new AutoMoqCustomization());
        Container.OmitAutoProperties = true;
        Container.Register<IScheduler>(() => Scheduler.Immediate);
        SetUp();
    }

    protected virtual void SetUp(){}
}