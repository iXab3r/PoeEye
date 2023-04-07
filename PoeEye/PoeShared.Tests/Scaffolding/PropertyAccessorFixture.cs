using System;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class PropertyAccessorFixture
{
    [Test]
    public void ShouldGetValue()
    {
        //Given
        var testClass = new TestClass() { StringProperty = "test" };
        var accessor = testClass.GetPropertyAccessor<string>(nameof(testClass.StringProperty));

        //When
        var value = accessor.Value;

        //Then
        value.ShouldBe("test");
    }

    [Test]
    public void ShouldGetValueForInner()
    {
        //Given
        var testClass = new TestClass() { Inner = new TestClass() { IntProperty = 3 }};
        var accessor = testClass.GetPropertyAccessor<int>($"{nameof(testClass.Inner)}.{nameof(testClass.IntProperty)}");

        //When
        var value = accessor.Value;

        //Then
        value.ShouldBe(3);
    }

    [Test]
    public void ShouldSetValue()
    {
        //Given
        var testClass = new TestClass() { StringProperty = "test" };
        var accessor = testClass.GetPropertyAccessor<string>(nameof(testClass.StringProperty));

        //When
        accessor.Value = "a";

        //Then
        testClass.StringProperty.ShouldBe("a");
        accessor.Value.ShouldBe("a");
    }

    [Test]
    public void ShouldThrowIfInvalidType()
    {
        //Given
        var testClass = new TestClass() { StringProperty = "test" };
        var accessor = testClass.GetPropertyAccessor<int>(nameof(testClass.StringProperty));

        //When
        var action = () => accessor.Value.SuppressWarning();

        //Then
        action.ShouldThrow<InvalidCastException>();
    }

    [Test]
    public void ShouldThrowOnUnknownProperty()
    {
        //Given
        var testClass = new TestClass();
        var accessor = testClass.GetPropertyAccessor<int>("non-existing property");

        //When
        var action = () => accessor.Value.SuppressWarning();

        //Then
        action.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ShouldThrowIfSourceIsNull()
    {
        //Given
        //When
        var action = () => new PropertyAccessor<string>(null!, "test").SuppressWarning();

        //Then
        action.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ShouldConvertIntToFloatOnGet()
    {
        //Given
        var testClass = new TestClass() { IntProperty = 3 };
        var accessor = new PropertyAccessor<double>(testClass, nameof(testClass.IntProperty));

        //When
        var result = accessor.Value;

        //Then
        result.ShouldBe(3);
    }

    [Test]
    public void ShouldNotConvertFloatToIntOnSet()
    {
        //Given
        var testClass = new TestClass() { IntProperty = 3 };
        var accessor = new PropertyAccessor<double>(testClass, nameof(testClass.IntProperty));

        //When
        Action action = () => accessor.Value = 4;

        //Then
        action.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ShouldExtractFromExpression()
    {
        //Given
        var testClass = new TestClass() { StringProperty = "test" };

        //When
        var accessor = testClass.GetPropertyAccessor(x => x.StringProperty);

        //Then
        accessor.Value.ShouldBe("test");
    }
    
    [Test]
    public void ShouldExtractFromExpressionForInnerObject()
    {
        //Given
        var testClass = new TestClass() { Inner = new TestClass() { StringProperty = "test" }};

        //When
        var accessor = testClass.GetPropertyAccessor(x => x.Inner.StringProperty);

        //Then
        accessor.Value.ShouldBe("test");
    }

    private sealed record TestClass
    {
        public string StringProperty { get; set; }
        
        public int IntProperty { get; set; }
        
        public double DoubleProperty { get; set; }
        
        public TestClass Inner { get; set; }
    }
}