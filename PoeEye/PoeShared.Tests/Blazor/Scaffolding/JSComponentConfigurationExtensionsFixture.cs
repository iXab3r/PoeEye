using System;
using AutoFixture;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Tests.Blazor.Scaffolding;

public class JSComponentConfigurationExtensionsFixture : FixtureBase
{
    private RootComponent store;

    protected override void SetUp()
    {
        base.SetUp();

        store = new RootComponent();
    }

    /// <summary>
    /// WHAT: Verifies that registered JS component metadata can be enumerated from the Blazor component store.
    /// HOW: Registers a component through the public Blazor API and reads it through the shared PoeShared helper.
    /// </summary>
    [Test]
    public void ShouldEnumerateRegisteredComponents()
    {
        // Given
        store.RegisterForJavaScript(typeof(BindAttributes), "binder");

        // When
        var registeredComponents = store.GetRegisteredJavaScriptComponents();

        // Then
        registeredComponents["binder"].ShouldBe(typeof(BindAttributes));
    }

    /// <summary>
    /// WHAT: Verifies that registered JS component metadata can be copied between Blazor component stores.
    /// HOW: Enumerates the source store through the shared helper and registers each component into another store.
    /// </summary>
    [Test]
    public void ShouldCopyRegisteredComponents()
    {
        // Given
        var targetStore = new RootComponent();
        store.RegisterForJavaScript(typeof(BindAttributes), "binder");

        // When
        foreach (var registeredComponent in store.GetRegisteredJavaScriptComponents())
        {
            targetStore.RegisterForJavaScriptIfMissing(registeredComponent.Value, registeredComponent.Key);
        }

        // Then
        targetStore.GetRegisteredJavaScriptComponents()["binder"].ShouldBe(typeof(BindAttributes));
    }

    /// <summary>
    /// WHAT: Verifies that repeated registration of the same identifier and component type is harmless.
    /// HOW: Calls the idempotent registration helper twice with the same component identifier and type.
    /// </summary>
    [Test]
    public void ShouldIgnoreDuplicateRegistrationOfSameType()
    {
        // Given
        var firstResult = store.RegisterForJavaScriptIfMissing(typeof(BindAttributes), "binder");

        // When
        var secondResult = store.RegisterForJavaScriptIfMissing(typeof(BindAttributes), "binder");

        // Then
        firstResult.ShouldBeTrue();
        secondResult.ShouldBeFalse();
        store.GetRegisteredJavaScriptComponents()["binder"].ShouldBe(typeof(BindAttributes));
    }

    /// <summary>
    /// WHAT: Verifies that conflicting JS component identifiers fail with a clear exception.
    /// HOW: Registers one component type and then attempts to reuse its identifier for another component type.
    /// </summary>
    [Test]
    public void ShouldRejectDuplicateRegistrationOfDifferentType()
    {
        // Given
        store.RegisterForJavaScriptIfMissing(typeof(BindAttributes), "binder");

        // When
        Action action = () => store.RegisterForJavaScriptIfMissing(typeof(RootComponent), "binder");

        // Then
        action.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("JS component identifier 'binder' is already registered");
    }

    private sealed class RootComponent : IJSComponentConfiguration
    {
        public JSComponentConfigurationStore JSComponents { get; } = new();
    }
}
