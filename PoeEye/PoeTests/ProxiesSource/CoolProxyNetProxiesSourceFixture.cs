namespace PoeEye.Tests.ProxiesSource
{
    using System.Linq;

    using NUnit.Framework;

    using ProxyProvider.ProxiesSource;

    using Shouldly;

    [TestFixture("Integration")]
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