using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using Microsoft.AspNetCore.Components;
using PoeShared.Blazor;
using PoeShared.Blazor.Services;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Blazor;

[TestFixture]
[SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.")]
public class BlazorContentPresenterFixture : FixtureBase
{
    private Mock<IBlazorViewRepository> viewRepositoryMock;

    protected override void SetUp()
    {
        viewRepositoryMock = Container.RegisterMock<IBlazorViewRepository>();
    }

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        var instance = CreateInstance();

        // Then
        instance.ViewRepository.ShouldBeSameAs(viewRepositoryMock.Object);
    }
    
    [Test]
    public void ShouldSetResolvedViewType_WhenViewTypeIsProvided()
    {
        // Given
        var expectedType = typeof(DummyComponent);
        var presenter = CreateInstance();
        presenter.ViewType = expectedType;

        // When 
        // Mimic lifecycle events like SetParametersAsync if needed.

        // Then
        presenter.ResolvedViewType.ShouldBe(expectedType);
    }

    [Test]
    public void ShouldResolveViewTypeFromRepository_WhenContentIsProvided()
    {
        // Given
        var content = new DummyContent();
        var expectedType = typeof(DummyComponent);
        viewRepositoryMock.Setup(repo => repo.ResolveViewType(content.GetType(), null)).Returns(expectedType);
        var presenter = CreateInstance();
        presenter.Content = content;

        // When 
        // Mimic lifecycle events like SetParametersAsync if needed.

        // Then
        presenter.ResolvedViewType.ShouldBe(expectedType);
    }
    
    [Test]
    public void ShouldDisposeOfOldView_WhenNewViewIsSet()
    {
        // Given
        var initialComponent = new DisposableDummyComponent();
        var presenter = CreateInstance();
        presenter.SetView(initialComponent);
        presenter.ViewType = typeof(DisposableDummyComponent); // Trigger the view change

        // When 
        presenter.SetView(new DisposableDummyComponent()); // Emulate setting a new component

        // Then
        initialComponent.IsDisposed.ShouldBeTrue();
    }

    [Test]
    public void ShouldDisposeOfComponent_WhenPresenterIsDisposed()
    {
        // Given
        var component = new DisposableDummyComponent();
        var presenter = CreateInstance();
        presenter.SetView(component);

        // When 
        presenter.Dispose();

        // Then
        component.IsDisposed.ShouldBeTrue();
    }

    private class DisposableDummyComponent : ComponentBase, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
    
    private class DummyComponent : ComponentBase { }
    private class DummyContent { }
    
    public BlazorContentPresenter CreateInstance()
    {
        var result = Container.Create<BlazorContentPresenter>();
        result.ViewRepository = viewRepositoryMock.Object;
        return result;
    }
}