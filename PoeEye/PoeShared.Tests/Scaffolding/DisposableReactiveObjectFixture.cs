using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using PoeShared.Scaffolding;
using ReactiveUI;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
{
    [TestFixture]
    public class DisposableReactiveObjectFixture
    {
        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
        }

        private Fixture fixture;

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        public void ShouldSubscribeToPropertyChange()
        {
            //Given
            var instance = CreateInstance();

            var values = new List<int>();
            using var anchor = instance.WhenAnyValue(x => x.IntProperty).Subscribe(x => values.Add(x));

            //When
            instance.IntProperty = 1;

            //Then
            values.ShouldBe(new[] { 0, 1 });
        }
        
        [Test]
        public void ShouldSubscribeToInnerClassPropertyChange()
        {
            //Given
            var instance = CreateInstance();
            instance.InnerValue = CreateInstance();

            var values = new List<int>();
            using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));

            //When
            instance.InnerValue.IntProperty = 1;

            //Then
            values.ShouldBe(new[] { 0, 1 });
        }
        
        [Test]
        public void ShouldSubscribeToInnerClassIfSetAfterSubscriptionOnPropertyChange()
        {
            //Given
            var instance = CreateInstance();

            var values = new List<int>();
            using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));

            //When
            var innerValue = CreateInstance();
            innerValue.IntProperty = 1;
            instance.InnerValue = innerValue;
            innerValue.IntProperty = 2;

            //Then
            values.ShouldBe(new[] { 1, 2 });
        }
        
        [Test]
        public void ShouldSubscribeToInnerClassIfNullOnPropertyChange()
        {
            // https://github.com/reactiveui/ReactiveUI/issues/976 Bindings with property chains not updating target when chain is broken
            // By design, WhenAny does not propagate null if part of chain is null, is simply ignores it until new instance becomes available
            
            //Given
            var instance = CreateInstance();
            instance.InnerValue = CreateInstance();

            var values = new List<int?>();
            using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));
            instance.InnerValue.IntProperty = 1;

            //When
            instance.InnerValue = default;

            //Then
            values.ShouldBe(new int?[] { 0, 1 });
        }

        private TestClass CreateInstance()
        {
            return fixture.Build<TestClass>().OmitAutoProperties().Create();
        }

        private sealed class TestClass : DisposableReactiveObject
        {
            private TestClass innerValue;
            private int intProperty;
            private string stringProperty;

            public int IntProperty
            {
                get => intProperty;
                set => RaiseAndSetIfChanged(ref intProperty, value);
            }

            public string StringProperty
            {
                get => stringProperty;
                set => RaiseAndSetIfChanged(ref stringProperty, value);
            }

            public TestClass InnerValue
            {
                get => innerValue;
                set => RaiseAndSetIfChanged(ref innerValue, value);
            }
        }
    }
}