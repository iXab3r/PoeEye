using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
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
        
        private sealed class Stub : BindableReactiveObject
        {
            private int doubleProperty;
            private Stub inner;
            private OtherStub innerOther;
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

            public int DoubleProperty
            {
                get => doubleProperty;
                set => RaiseAndSetIfChanged(ref doubleProperty, value);
            }

            public OtherStub InnerOther
            {
                get => innerOther;
                set => RaiseAndSetIfChanged(ref innerOther, value);
            }

            public Stub Inner
            {
                get => inner;
                set => RaiseAndSetIfChanged(ref inner, value);
            }
        }

        private sealed class OtherStub : BindableReactiveObject
        {
            private int doubleProperty;

            private Stub inner;

            private OtherStub innerOther;

            public int DoubleProperty
            {
                get => doubleProperty;
                set => RaiseAndSetIfChanged(ref doubleProperty, value);
            }


            public OtherStub InnerOther
            {
                get => innerOther;
                set => RaiseAndSetIfChanged(ref innerOther, value);
            }

            public Stub Inner
            {
                get => inner;
                set => RaiseAndSetIfChanged(ref inner, value);
            }
        }
    }
}