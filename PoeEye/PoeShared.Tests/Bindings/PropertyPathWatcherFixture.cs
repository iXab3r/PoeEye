using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Bindings;
using Shouldly;

namespace PoeShared.Tests.Bindings
{
    [TestFixture]
    public class PropertyPathWatcherFixture : FixtureBase
    {
        [Test]
        public void ShouldCreateFromString()
        {
            //Given
            var instance = new PropertyPathWatcher();
            instance.PropertyPath = nameof(TestContainer.IntProperty);
            instance.HasValue.ShouldBe(false);
            instance.Value.ShouldBe(default);

            //When
            instance.Source = new TestContainer() { IntProperty = 1 };

            //Then
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(1);
        }

        [Test]
        public void ShouldUnbindFromString()
        {
            //Given
            var instance = new PropertyPathWatcher();
            instance.PropertyPath = nameof(TestContainer.IntProperty);
            instance.Source = new TestContainer() { IntProperty = 1 };
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(1);
            
            //When
            instance.Source = default;

            //Then
            instance.HasValue.ShouldBe(false);
            instance.Value.ShouldBe(default);
        }

        [Test]
        public void ShouldBindToInnerFromString()
        {
            //Given
            var instance = new PropertyPathWatcher();
            instance.PropertyPath = $"{nameof(TestContainer.Inner)}.{nameof(TestContainer.IntProperty)}";

            var source = new TestContainer() { IntProperty = 1 };
            instance.Source = source;

            instance.HasValue.ShouldBe(false);
            instance.Value.ShouldBe(default);

            //When
            source.Inner = new TestContainer() { IntProperty = 2 };

            //Then
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(2);
        }

        [Test]
        public void ShouldRebindWhenPropertyPathChanges()
        {
            //Given
            var instance = new PropertyPathWatcher();
            instance.Source = new TestContainer() { IntProperty = 1, Inner = new TestContainer() { IntProperty = 2}};
            instance.PropertyPath = nameof(TestContainer.IntProperty);
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(1);
            
            //When
            instance.PropertyPath = $"{nameof(TestContainer.Inner)}.{nameof(TestContainer.IntProperty)}";

            //Then
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(2);
        }

        [Test]
        public void ShouldSetCurrentValueFromString()
        {
            //Given
            var instance = new PropertyPathWatcher();
            instance.Source = new TestContainer() { IntProperty = 1, Inner = new TestContainer() { IntProperty = 2}};
            instance.PropertyPath = $"{nameof(TestContainer.Inner)}.{nameof(TestContainer.IntProperty)}";
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(2);
            
            //When
            instance.SetCurrentValue(3);

            //Then
            instance.HasValue.ShouldBe(true);
            instance.Value.ShouldBe(3);
        }
    }
}