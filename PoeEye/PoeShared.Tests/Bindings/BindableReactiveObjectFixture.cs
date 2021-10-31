using System.Linq;
using Moq;
using NUnit.Framework;
using PoeShared.Bindings;
using PoeShared.Tests.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Bindings
{
    [TestFixture]
    public class AuraModelFixture : FixtureBase
    {
        [Test]
        public void ShouldBindPropertiesOnTheSameObject()
        {
            //Given
            var source = new Stub() { IntProperty = 3 };
            source.AddOrUpdateBinding(nameof(source.DoubleProperty), source, nameof(source.IntProperty));
            source.DoubleProperty.ShouldBe(3);

            //When
            source.IntProperty = 4;

            //Then
            source.DoubleProperty.ShouldBe(4);
        }

        [Test]
        public void ShouldBindOnInner()
        {
            //Given
            var source = new Stub();
            source.InnerOther = new OtherStub()
            {
                Inner = new Stub()
                {
                    IntProperty = 4
                }
            };

            source.AddOrUpdateBinding(nameof(source.DoubleProperty), source, "InnerOther.Inner.IntProperty");
            source.DoubleProperty.ShouldBe(4);
            
            //When
            source.InnerOther.Inner.IntProperty = 5;

            //Then
            source.DoubleProperty.ShouldBe(5);
        }

        [Test]
        public void ShouldBindOnExpression()
        {
            //Given
            var source = new Stub();
            var target = new OtherStub();
            target.AddOrUpdateBinding(x => x.IntProperty, source, x => x.IntProperty + 1);
            target.IntProperty.ShouldBe(1);

            //When
            source.IntProperty = 2;

            //Then
            target.IntProperty.ShouldBe(3);
        }

        [Test]
        public void ShouldDisposeBindingOnRemovel()
        {
            //Given
            var source = new Stub();
            var initialBinding = Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "key");
            source.AddOrUpdateBinding(initialBinding);
            source.Bindings.Items.ShouldContain(initialBinding);

            //When
            source.RemoveBinding(initialBinding);

            //Then
            initialBinding.GetMock().Verify(x => x.Dispose());
            source.Bindings.Items.ShouldNotContain(initialBinding);
        }

        [Test]
        public void ShouldUpdateBinding()
        {
            //Given
            var source = new Stub();

            var initialBinding = Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "key");
            var updatedBinding = Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "key");
            source.AddOrUpdateBinding(initialBinding);
            source.Bindings.Items.ShouldContain(initialBinding);

            //When
            source.AddOrUpdateBinding(updatedBinding);

            //Then
            initialBinding.GetMock().Verify(x => x.Dispose());
            source.Bindings.Items.ShouldNotContain(initialBinding);
            source.Bindings.Items.ShouldContain(updatedBinding);
        }

        [Test]
        [TestCase("1", "1")]
        [TestCase("1.1", "1")]
        [TestCase("1", "1.1")]
        public void ShouldOverwriteIfParentIsBinding(string initial, string updated)
        {
            //Given
            var source = new Stub();

            var initialBinding = Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == initial);
            var updatedBinding = Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == updated);
            source.AddOrUpdateBinding(initialBinding);
            source.Bindings.Items.ShouldContain(initialBinding);

            //When
            source.AddOrUpdateBinding(updatedBinding);

            //Then
            initialBinding.GetMock().Verify(x => x.Dispose());
            source.Bindings.Items.ShouldNotContain(initialBinding);
            source.Bindings.Items.ShouldContain(updatedBinding);
        }

        [Test]
        [TestCase("", "1 2.1.1 3.2")]
        [TestCase("4", "1 2.1.1 3.2")]
        [TestCase("1", "2.1.1 3.2")]
        [TestCase("2", "1 3.2")]
        [TestCase("2.1", "1 3.2")]
        [TestCase("2.1.1", "1 3.2")]
        [TestCase("3", "1 2.1.1")]
        public void ShouldRemoveNestedBindings(string whatToRemove, string expected)
        {
            //Given
            var source = new Stub();
            source.AddOrUpdateBinding(Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "1"));
            source.AddOrUpdateBinding(Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "2"));
            source.AddOrUpdateBinding(Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "2.1.1"));
            source.AddOrUpdateBinding(Mock.Of<IReactiveBinding>(x => x.TargetPropertyPath == "3.2"));

            //When
            source.RemoveBinding(whatToRemove);

            //Then
            var items = string.Join(" ",  source.BindingsList.Select(x => x.TargetPropertyPath).OrderBy(x => x));
            items.ShouldBe(expected);
        }
        
        private sealed class Stub : BindableReactiveObject
        {
            public int IntProperty { get; set; }

            public string StringProperty { get; set; }

            public int DoubleProperty { get; set; }

            public OtherStub InnerOther { get; set; }

            public Stub Inner { get; set; }
        }

        private sealed class OtherStub : BindableReactiveObject
        {
            public int IntProperty { get; set; }

            public OtherStub InnerOther { get; set; }

            public Stub Inner { get; set; }
        }
    }
}