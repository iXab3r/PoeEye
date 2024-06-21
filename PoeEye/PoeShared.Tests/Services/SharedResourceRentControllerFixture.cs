using System.Reactive.Linq;
using System.Threading.Tasks;
using PoeShared.Services;

namespace PoeShared.Tests.Services;

public class SharedResourceRentControllerFixture : FixtureBase
{
    [Test]
    public async Task ShouldRent()
    {
        //Given
        var instance = CreateInstance();

        //When
        using var rent = instance.Rent("test");


        //Then
        var isRented = await instance.IsRented.Take(1);
        isRented.ShouldBe(new AnnotatedBoolean(true, "test"));
    }

    [Test]
    public async Task ShouldRelease()
    {
        //Given
        var instance = CreateInstance();
        var rent = instance.Rent("test");

        //When
        rent.Dispose();

        //Then
        var isRented = await instance.IsRented.Take(1);
        isRented.ShouldBe(default);
    }

    private SharedResourceRentController CreateInstance()
    {
        return new SharedResourceRentController();
    }
}