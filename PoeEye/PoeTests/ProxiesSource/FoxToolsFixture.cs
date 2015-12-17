namespace PoeEye.Tests.ProxiesSource
{
    using System.Linq;

    using NUnit.Framework;

    using ProxyProvider.ProxiesSource.FoxTools;

    using Shouldly;

    [TestFixture("Integration")]
    internal sealed class FoxToolsFixture
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

        private FoxToolsProxiesSource CreateInstance()
        {
            return new FoxToolsProxiesSource();
        }
    }
}