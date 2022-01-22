using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using PoeShared.Bindings;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Bindings;

[TestFixture]
public class ReactiveBindingFixture : FixtureBase
{

    [Test]
    public void ShouldBind()
    {
        //Given
        var instance = new ReactiveBinding<SourceContainer, TargetContainer, int>(x => x.IntProperty, x => x.IntProperty);
        instance.Source = new SourceContainer();
        instance.Target = new TargetContainer();

        //When
        instance.Source.IntProperty = 1;

        //Then
        instance.Target.IntProperty.ShouldBe(1);
    }

    [Test]
    public void ShouldBindToExpression()
    {
        //Given
        var instance = new ReactiveBinding<SourceContainer, TargetContainer, int>(x => x.IntProperty, x => x.IntProperty + 1);
        instance.Source = new SourceContainer();
        instance.Target = new TargetContainer();
        instance.Target.IntProperty.ShouldBe(1);

        //When
        instance.Source.IntProperty = 1;

        //Then
        instance.Target.IntProperty.ShouldBe(2);
    }

    [Test]
    public void ShouldNotThrowWhenInvalidSetter()
    {
        //Given
        //When
        var instance = new ReactiveBinding<SourceContainer, SourceContainer, int>(x => x.ReadOnlyIntProperty, x => x.IntProperty + 1);
        instance.Source = new SourceContainer();
        instance.Target = new SourceContainer();

        //Then
        instance.SourceWatcher.HasValue.ShouldBe(true);
        instance.TargetWatcher.HasValue.ShouldBe(true);
        instance.TargetWatcher.SupportsSetValue.ShouldBe(false);
        instance.TargetWatcher.CanSetValue.ShouldBe(false);
    }
        
    public sealed class SourceContainer : DisposableReactiveObject
    {
        public int IntProperty { get; set; }
            
        public int ReadOnlyIntProperty { get; }

        public SourceContainer Inner { get; set; }
    }
        
    public sealed class TargetContainer : DisposableReactiveObject
    {
        public int IntProperty { get; set; }

        public TargetContainer Inner { get; set; }
    }
}