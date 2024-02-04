using AutoFixture;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Wpf.Scaffolding;

namespace PoeShared.Tests.Blazor.Wpf.Scaffolding;

/// <summary>
/// This class is needed to enumerate over internal list of JS Custom components. Officially there is no way to get the list of components which were registered
/// Should be removed when MS will provide an official way
/// </summary>
public class JSComponentConfigurationStoreAccessorFixture : FixtureBase
{
    private RootComponent store;
    
    protected override void SetUp()
    {
        base.SetUp();

        store = new RootComponent();
    }

    [Test]
    public void ShouldCreate()
    {
        //Given
        //When
        var action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldRegister()
    {
        //Given
        var instance = CreateInstance();

        //When
        store.RegisterForJavaScript(typeof(BindAttributes), "binder");

        //Then
        instance.JsComponentTypesByIdentifier["binder"].ShouldBe(typeof(BindAttributes));
    }

    private JSComponentConfigurationStoreAccessor CreateInstance()
    {
        return new JSComponentConfigurationStoreAccessor(store.JSComponents);
    }
    
    private sealed class RootComponent : IJSComponentConfiguration
    {
        public JSComponentConfigurationStore JSComponents { get; } = new();
    }
}