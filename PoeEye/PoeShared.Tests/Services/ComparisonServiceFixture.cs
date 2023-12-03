using System;
using System.Collections.Generic;
using System.Numerics;
using PoeShared.Services;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Services;

[TestFixture]
internal class ComparisonServiceFixtureTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    [TestCaseSource(nameof(ShouldCompareCases))]
    public void ShouldCompare(object class1, object class2, bool expectedEquality)
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.Compare(class1, class2);

        //Then
        result.AreEqual.ShouldBe(expectedEquality);
    }

    public static IEnumerable<TestCaseData> ShouldCompareCases()
    {
        yield return new NamedTestCaseData(null, null, true) {TestName = "Null and null should be equal"};
        yield return new NamedTestCaseData(new TestClass() {IntValue = 1}, null, false) {TestName = "Null and non-null should not be equal"};
        yield return new NamedTestCaseData(null, new TestClass() {IntValue = 1}, false) {TestName = "Non-null and null should not be equal"};
        yield return new NamedTestCaseData(new TestClass() {IntValue = 1}, new TestClass() {IntValue = 1}, true) {TestName = "Integers, equal"};
        yield return new NamedTestCaseData(new TestClass() {IntValue = 1}, new TestClass() {IntValue = 2}, false) {TestName = "Integers, non-equal"};
        yield return new NamedTestCaseData(new TestClass() {IntArray = new[] {1, 2}}, new TestClass() {IntArray = new[] {1, 2}}, true) {TestName = "Integers arrays, equal"};
        yield return new NamedTestCaseData(new TestClass() {IntArray = new[] {1, 2}}, new TestClass() {IntArray = new[] {1, 3}}, false) {TestName = "Integers arrays different elements, non-equal"};
        yield return new NamedTestCaseData(new TestClass() {IntArray = new[] {1}}, new TestClass() {IntArray = new[] {1, 2}}, false) {TestName = "Integers arrays different number of elements, non-equal"};

        // String comparisons
        yield return new NamedTestCaseData(new TestClass() {StringValue = "Hello"}, new TestClass() {StringValue = "Hello"}, true) {TestName = "Strings, equal"};
        yield return new NamedTestCaseData(new TestClass() {StringValue = "Hello"}, new TestClass() {StringValue = "World"}, false) {TestName = "Strings, non-equal"};

        // Decimal comparisons
        yield return new NamedTestCaseData(new TestClass() {DecimalValue = 10.5m}, new TestClass() {DecimalValue = 10.5m}, true) {TestName = "Decimals, equal"};
        yield return new NamedTestCaseData(new TestClass() {DecimalValue = 10.5m}, new TestClass() {DecimalValue = 20.5m}, false) {TestName = "Decimals, non-equal"};

        // Double comparisons with precision consideration
        yield return new NamedTestCaseData(new TestClass() {DoubleValue = 10.123}, new TestClass() {DoubleValue = 10.123}, true) {TestName = "Doubles, equal"};
        yield return new NamedTestCaseData(new TestClass() {DoubleValue = 10.123}, new TestClass() {DoubleValue = 10.124}, true) {TestName = "Doubles precision-3, equal"};
        yield return new NamedTestCaseData(new TestClass() {DoubleValue = 10.123}, new TestClass() {DoubleValue = 10.134}, false) {TestName = "Doubles precision-2, non-equal"};
        yield return new NamedTestCaseData(new TestClass() {DoubleValue = 10.123}, new TestClass() {DoubleValue = 10.234}, false) {TestName = "Doubles precision-1, non-equal"};

        // List of strings
        yield return new NamedTestCaseData(new TestClass() {StringList = new List<string> {"one", "two"}}, new TestClass() {StringList = new List<string> {"one", "two"}}, true) {TestName = "String lists, equal"};
        yield return new NamedTestCaseData(new TestClass() {StringList = new List<string> {"one", "two"}}, new TestClass() {StringList = new List<string> {"two", "one"}}, false) {TestName = "String lists, non-equal order"};
        yield return new NamedTestCaseData(new TestClass() {StringList = new List<string> {"one", "two"}}, new TestClass() {StringList = new List<string> {"one", "three"}}, false) {TestName = "String lists, different elements"};

        // System.Drawing.Point comparisons
        yield return new NamedTestCaseData(new TestClass() {DrawingPointValue = new System.Drawing.Point(5, 5)}, new TestClass() {DrawingPointValue = new System.Drawing.Point(5, 5)}, true) {TestName = "System.Drawing.Point, equal"};
        yield return new NamedTestCaseData(new TestClass() {DrawingPointValue = new System.Drawing.Point(5, 5)}, new TestClass() {DrawingPointValue = new System.Drawing.Point(10, 5)}, false) {TestName = "System.Drawing.Point, non-equal"};

        // Vector2 comparisons
        yield return new NamedTestCaseData(new TestClass() {Vector2Value = new Vector2(1.0f, 2.0f)}, new TestClass() {Vector2Value = new Vector2(1.0f, 2.0f)}, true) {TestName = "Vector2, equal"};
        yield return new NamedTestCaseData(new TestClass() {Vector2Value = new Vector2(1.0f, 2.0f)}, new TestClass() {Vector2Value = new Vector2(2.0f, 2.0f)}, false) {TestName = "Vector2, non-equal"};
        
        // Vector3 comparisons
        yield return new NamedTestCaseData(new TestClass() {Vector3Value = new Vector3(1.0f, 2.0f, 3.0f)}, new TestClass() {Vector3Value = new Vector3(1.0f, 2.0f, 3.0f)}, true) {TestName = "Vector3, equal"};
        yield return new NamedTestCaseData(new TestClass() {Vector3Value = new Vector3(1.0f, 2.0f, 3.0f)}, new TestClass() {Vector3Value = new Vector3(2.0f, 2.0f, 3.0f)}, false) {TestName = "Vector3, non-equal"};
        
        // Matrix3x2 comparisons
        yield return new NamedTestCaseData(new TestClass() {Matrix3x2Value = new Matrix3x2(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f)}, new TestClass() {Matrix3x2Value = new Matrix3x2(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f)}, true) {TestName = "Matrix3x2, equal"};
        yield return new NamedTestCaseData(new TestClass() {Matrix3x2Value = new Matrix3x2(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f)}, new TestClass() {Matrix3x2Value = new Matrix3x2(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 7.0f)}, false) {TestName = "Matrix3x2, non-equal"};
    }

    private ComparisonService CreateInstance()
    {
        return new ComparisonService();
    }

    public sealed record TestClass
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public int[] IntArray { get; set; }
        public List<string> StringList { get; set; }
        public System.Drawing.Point DrawingPointValue { get; set; }
        public Vector2 Vector2Value { get; set; }
        public Vector3 Vector3Value { get; set; }
        public Matrix3x2 Matrix3x2Value { get; set; }
    }
}