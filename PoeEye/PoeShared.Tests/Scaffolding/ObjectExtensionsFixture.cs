using NUnit.Framework;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class ObjectExtensionsFixture
{
    [Test]
    [TestCase(null, null, null, "123")]
    [TestCase(null, ", ", null, "1, 2, 3")]
    [TestCase("ints", ", ", null, "ints(3):1, 2, 3")]
    [TestCase(null, null, 2, "12and 1 more...")]
    [TestCase(null, ", ", 2, "1, 2, and 1 more...")]
    [TestCase("ints", ", ", 2, "ints(3): 1, 2, and 1 more...")]
    [TestCase("ints", ", ", null, "ints(3): 1, 2, 3")]
    public void ShouldDumpToTable(string name, string separator, int? maxItemsToShow, string expected)
    {
        //Given
        var input = new int[] { 1, 2, 3 };

        //When
        var result = input.DumpToTable(
            name: name,
            separator: separator,
            maxItemsToShow: maxItemsToShow);

        //Then

    }
}