using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Numerics;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class ExpressionUtilsFixture : FixtureBase
{
    [Test]
    public void ShouldParseSimpleName()
    {
        // Given
        var test = new TestClass { Name = "Foo" };

        // When
        var (root, path) = ExpressionUtils.ParseBindingExpression(() => test.Name);

        // Then
        path.ShouldBe("Name");
        root.Compile().Invoke().ShouldBe(test);
    }

    [Test]
    public void ShouldParseNestedProperty()
    {
        // Given
        var test = new TestClass
        {
            Position = new Vector2(10, 20)
        };

        // When
        var (root, path) = ExpressionUtils.ParseBindingExpression(() => test.Position.X);

        // Then
        path.ShouldBe("Position.X");
        root.Compile().Invoke().ShouldBe(test);
    }

    private sealed class TestClass
    {
        public Vector2 Position { get; set; }

        public string Name { get; set; }
    }
}
