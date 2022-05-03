using Moq;

namespace PoeShared.Tests.Scaffolding;

public static class ObjectExtensions
{
    public static Mock<T> GetMock<T>(this T mocked) where T : class
    {
        return Mock.Get(mocked);
    }

    /// <summary>
    ///  Does nothing - used to make compiler job a bit easier for tests with action () => return something
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    public static void NoOp<T>(this T source)
    {
    }
}