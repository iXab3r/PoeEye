using Newtonsoft.Json;
using PoeShared.IO;

namespace PoeShared.Tests.IO;

[TestFixture]
internal class OSPathConverterFixtureTests : FixtureBase
{
    protected override void SetUp()
    {
    }

    [Test]
    public void ShouldSerialize()
    {
        //Given
        var input = new TestClass()
        {
            Path = new OSPath("test"),
            PathAsString = "abc"
        };

        //When
        var result = JsonConvert.SerializeObject(input);

        //Then
        result.ShouldBe("""{"Path":"test","PathAsString":"abc"}""");
    }

    [Test]
    public void ShouldDeserialize()
    {
        //Given
        var input = """{"Path":"test","PathAsString":"abc"}""";

        //When
        var result = JsonConvert.DeserializeObject<TestClass>(input);

        //Then
        result.PathAsString.ShouldBe("abc");
        result.Path.FullPath.ShouldBe("test");
    }

    public record TestClass
    {
        public OSPath Path { get; init; }
        
        public string PathAsString { get; init; }
    }
}