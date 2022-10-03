using System.Collections.Generic;
using AutoFixture;
using Newtonsoft.Json;
using NUnit.Framework;
using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity;

[TestFixture]
internal class PoeSharedContractResolverTests : FixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldResolveCases))]
    public void ShouldResolve(SampleClass sample, string expected)
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = JsonConvert.SerializeObject(sample, new JsonSerializerSettings()
        {
            ContractResolver = instance,
            Formatting = Formatting.None,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        });

        //Then
        result.ShouldBe(expected);
    }

    public static IEnumerable<TestCaseData> ShouldResolveCases()
    {
        yield return new TestCaseData(new SampleClass(), "{}");
        yield return new TestCaseData(new SampleClass(){ StringValue = "str"}, "{\"StringValue\":\"str\"}");
        yield return new TestCaseData(new SampleClass(){ IntValue = 1}, "{\"IntValue\":1}");
        yield return new TestCaseData(new SampleClass(){ IntArray = new[] { 1, 2 }}, "{\"IntArray\":[1,2]}");
        yield return new TestCaseData(new SampleClass(){ IntList = new() { 1, 2 }}, "{\"IntList\":[1,2]}");
        yield return new TestCaseData(new SampleClass(){ ReadonlyIntList = new[] { 1, 2 }}, "{\"ReadonlyIntList\":[1,2]}");
        yield return new TestCaseData(new SampleClass(){ ReadonlyIntSet = new HashSet<int>(new[] { 1, 2 })}, "{\"ReadonlyIntSet\":[1,2]}");
        yield return new TestCaseData(new SampleClass(){ Container = new IntContainer(1) }, "{\"Container\":{\"Value\":1}}");
        yield return new TestCaseData(new SampleClass(){ ContainerArray = new[]{ new IntContainer(1), new IntContainer(2) } }, "{\"ContainerArray\":[{\"Value\":1},{\"Value\":2}]}");
        yield return new TestCaseData(new SampleClass(){ ContainerList = new List<IntContainer>() { new IntContainer(1), new IntContainer(2) } }, "{\"ContainerList\":[{\"Value\":1},{\"Value\":2}]}");
        yield return new TestCaseData(new SampleClass(){ ReadonlyContainerList = new List<IntContainer>() { new IntContainer(1), new IntContainer(2) } }, "{\"ReadonlyContainerList\":[{\"Value\":1},{\"Value\":2}]}");
    }

    public PoeSharedContractResolver CreateInstance()
    {
        return Container.Create<PoeSharedContractResolver>();
    }

    public sealed record SampleClass
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public int[] IntArray { get; set; }
        public List<int> IntList { get; set; }
        public IReadOnlyList<int> ReadonlyIntList { get; set; }
        public IReadOnlySet<int> ReadonlyIntSet { get; set; }
        public IntContainer Container { get; set; }
        public IntContainer[] ContainerArray { get; set; }
        public List<IntContainer> ContainerList { get; set; }
        public IReadOnlyList<IntContainer> ReadonlyContainerList { get; set; }
    }

    public sealed record IntContainer(int Value)
    {
    }
}