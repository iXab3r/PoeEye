using Moq;

namespace PoeShared.Tests.Scaffolding
{
    public static class ObjectExtensions
    {
        public static Mock<T> GetMock<T>(this T mocked) where T : class
        {
            return Mock.Get(mocked);
        }
    }
}