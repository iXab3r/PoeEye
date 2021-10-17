using System;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using PoeShared.Bindings;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Bindings
{
    [TestFixture]
    public class ExpressionWatcherFixture : FixtureBase
    {
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
        public void ShouldWatch()
        {
            //Given
            var instance = CreateInstance();
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);

            //When
            instance.Source = new TestContainer() { IntProperty = 1 };

            //Then
            instance.Value.ShouldBe(1);
            instance.HasValue.ShouldBe(true);
        }

        [Test]
        public void ShouldWatchExpression()
        {
            //Given
            var instance = new ExpressionWatcher<TestContainer, int>(x => x.Inner.IntProperty + 1);
            instance.Source = new TestContainer() { Inner = new TestContainer() { IntProperty = 1 }};
            instance.Value.ShouldBe(2);
            instance.HasValue.ShouldBe(true);
            
            //When
            instance.Source.Inner.IntProperty = 4;

            //Then
            instance.Value.ShouldBe(5);
            instance.HasValue.ShouldBe(true);
        }

        [Test]
        public void ShouldNotSupportSettingFields()
        {
            //Given
            //When
            var instance = new ExpressionWatcher<TestContainer, int>(x => x.intField);

            //Then
            instance.CanSetValue.ShouldBeFalse();
        }

        [Test]
        public void ShouldResetWhenUnbind()
        {
            //Given
            var instance = CreateInstance();
            instance.Source = new TestContainer() { IntProperty = 1 };
            instance.Value.ShouldBe(1);
            instance.HasValue.ShouldBe(true);

            //When
            instance.Source = default;

            //Then
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);
        }

        [Test]
        public void ShouldSetCurrentValue()
        {
            //Given
            var instance = CreateInstance();
            instance.Source = new TestContainer() { IntProperty = 1 };
            instance.Value.ShouldBe(1);
            instance.HasValue.ShouldBe(true);

            //When
            instance.SetCurrentValue(2);

            //Then
            instance.Value.ShouldBe(2);
            instance.HasValue.ShouldBe(true);
        }

        [Test]
        public void ShouldSupportExpressions()
        {
            //Given
            var instance = new ExpressionWatcher<TestContainer, int>(x => x.IntProperty + 1);
            
            //When
            instance.Source = new TestContainer() { IntProperty = 1 };

            //Then
            instance.Value.ShouldBe(2);
            instance.HasValue.ShouldBe(true);
        }

        [Test]
        public void ShouldSupportCollections()
        {
            //Given
            var instance = new ExpressionWatcher<TestReactiveContainer, int>(x => x.Containers.Where(y => y.Id == "b").Select(y => y.IntProperty + 1).FirstOrDefault());
            instance.Source = new TestReactiveContainer();
            
            instance.Source.Containers.Add(new TestContainer() { IntProperty = 1, Id = "a" });
            instance.Value.ShouldBe(0);
            instance.HasValue.ShouldBe(true);
            
            //When
            instance.Source.Containers.Add(new TestContainer() { IntProperty = 2, Id = "b" });


            //Then
            instance.Value.ShouldBe(3);
            instance.HasValue.ShouldBe(true);
        }
        
        [Test]
        public void ShouldNotThrowWhenElementInCollectionNotFound()
        {
            //Given
            var instance = new ExpressionWatcher<TestReactiveContainer, int>(
                x => x.Containers.Where(y => y.Id == "b").Select(y => y.IntProperty + 1).First(),
                x => x != null && x.Containers.Any(y => y.Id == "b"));
            instance.Source = new TestReactiveContainer();
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);
            
            //When
            instance.Source.Containers.Add(new TestContainer() { IntProperty = 2, Id = "b" });

            //Then
            instance.Value.ShouldBe(3);
            instance.HasValue.ShouldBe(true);
        }
        
        [Test]
        public void ShouldBindToCollectionsFromString()
        {
            //Given
            var instance = new ExpressionWatcher(typeof(int));
            instance.SourceExpression = @"x.Containers.Where(y => y.Id == ""b"").Select(y => y.IntProperty + 1).First()";
            instance.ConditionExpression = @"x != null && x.Containers.Any(y => y.Id == ""b"")";
            var source = new TestReactiveContainer();
            instance.Source = source;
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);
            instance.CanSetValue.ShouldBe(false);
            
            //When
            source.Containers.Add(new TestContainer() { IntProperty = 2, Id = "b" });

            //Then
            instance.Value.ShouldBe(3);
            instance.HasValue.ShouldBe(true);
            instance.CanSetValue.ShouldBe(false);
        }

        [Test]
        public void ShouldBindToString()
        {
            //Given
            var instance = new ExpressionWatcher(typeof(string));
            instance.SourceExpression = @"x.Containers.Where(y => y.Id == ""b"").Select(y => y.Id).First()";
            instance.ConditionExpression = @"x != null && x.Containers.Any(y => y.Id == ""b"")";
            var source = new TestReactiveContainer();
            source.Containers.Add(new TestContainer() { Id = "b" });
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);
            instance.CanSetValue.ShouldBe(false);

            //When
            instance.Source = source;

            //Then
            instance.Value.ShouldBe("b");
            instance.HasValue.ShouldBe(true);
            instance.CanSetValue.ShouldBe(false);
        }
        
        [Test]
        public void ShouldSupportSetValueForCollectionBinding()
        {
            //Given
            var instance = new ExpressionWatcher(typeof(int));
            instance.SourceExpression = @"x.Containers.Where(y => y.Id == ""b"").First().IntProperty";
            instance.ConditionExpression = @"x != null && x.Containers.Any(y => y.Id == ""b"")";

            var source = new TestReactiveContainer();
            instance.Source = source;
            instance.Value.ShouldBe(default);
            instance.HasValue.ShouldBe(false);
            instance.CanSetValue.ShouldBe(false);
            source.Containers.Add(new TestContainer() { IntProperty = 2, Id = "b" });
            instance.Value.ShouldBe(2);
            instance.HasValue.ShouldBe(true);
            instance.CanSetValue.ShouldBe(true);

            //When
            instance.SetCurrentValue(3);

            //Then
            instance.Value.ShouldBe(2); // FIXME Target value is not updated automatically if we're binding to collection
            instance.HasValue.ShouldBe(true);
        }

        private ExpressionWatcher<TestContainer, int> CreateInstance()
        {
            return new ExpressionWatcher<TestContainer, int>(x => x.IntProperty);
        }
    }
}