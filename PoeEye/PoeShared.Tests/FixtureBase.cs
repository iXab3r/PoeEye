﻿using System;
using System.Reactive.Concurrency;
using System.Threading;
using NUnit.Framework;
using AutoFixture;
using AutoFixture.AutoMoq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Tests;

public abstract class FixtureBase
{
    public Fixture Container { get; private set; }
    public IUnityContainer UnityContainer { get; private set; }
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

        UnityContainer = new UnityContainer();
        Container.Register(() => UnityContainer);
        Container.Register(() => Log);

        if (!SchedulerProvider.Instance.TryGet(WellKnownSchedulers.Background, out var _))
        {
            SchedulerProvider.Instance.Add(WellKnownSchedulers.Background, Scheduler.Default);
        }
        if (!SchedulerProvider.Instance.TryGet(WellKnownSchedulers.RedirectToUI, out var _))
        {
            SchedulerProvider.Instance.Add(WellKnownSchedulers.RedirectToUI, Scheduler.Immediate);
        }

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
}