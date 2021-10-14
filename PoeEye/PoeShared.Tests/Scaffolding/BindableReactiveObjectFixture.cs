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

            public int IntProperty { get; set; }

            public string StringProperty { get; set; }

            public int DoubleProperty { get; set; }

            public OtherStub InnerOther { get; set; }

            public Stub Inner { get; set; }
        }

        private sealed class OtherStub : BindableReactiveObject
        {

            public int DoubleProperty { get; set; }


            public OtherStub InnerOther { get; set; }

            public Stub Inner { get; set; }
        }
    }
}