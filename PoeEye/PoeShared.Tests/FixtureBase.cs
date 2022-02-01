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

    protected FixtureBase()
    {
        Log = GetType().PrepareLogger();
    }

    [SetUp]
    public void SetUpTest()
    {
        Container = new Fixture();
        Container.Customize(new AutoMoqCustomization());
        Container.OmitAutoProperties = true;
        SetUp();
    }

    protected virtual void SetUp(){}
}