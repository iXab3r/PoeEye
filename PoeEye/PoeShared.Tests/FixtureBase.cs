using NUnit.Framework;
using AutoFixture;
using AutoFixture.AutoMoq;

namespace PoeShared.Tests;

public abstract class FixtureBase
{
    public Fixture Container { get; private set; }

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