namespace PoeEye.Tests
{
    using System.Linq;

    using NUnit.Framework;

    using ProxyProvider;

    using Shouldly;

    [TestFixture()]
    public sealed class CoolProxyNetProxiesSourceFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void ShouldRequestProxiesList()
        {
            //Given
            var instance = CreateInstance();

            //When
            var proxies = instance.GetProxies();

            //Then
            proxies.Count().ShouldBeGreaterThan(0);
        }

        private CoolProxyNetProxiesSource CreateInstance()
        {
            return new CoolProxyNetProxiesSource();
        }
    }
}