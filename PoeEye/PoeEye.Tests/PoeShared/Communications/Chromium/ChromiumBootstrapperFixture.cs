using CefSharp.OffScreen;
using Moq;
using NUnit.Framework;
using PoeShared.Chromium.Communications;
using PoeShared.Prism;

namespace PoeEye.Tests.PoeShared.Communications.Chromium
{
    [TestFixture]
    [Ignore("Chromium is currently not testably w/o hacks")]
    public class ChromiumBootstrapperFixture
    {
        [SetUp]
        public void SetUp()
        {
            browserFactory = new Mock<IFactory<ChromiumBrowser, ChromiumWebBrowser>>();
        }

        private Mock<IFactory<ChromiumBrowser, ChromiumWebBrowser>> browserFactory;

        [TestFixtureSetUp]
        public void SetUpFixture()
        {
        }

        private ChromiumBrowserFactory CreateInstance()
        {
            return new ChromiumBrowserFactory(browserFactory.Object);
        }


        [Test]
        public void ShouldCreate()
        {
            //Then
            CreateInstance();
        }

        [Test]
        public void ShouldLoadPage()
        {
            //Given
            var instance = CreateInstance();
            var browser = instance.CreateBrowser();

            //When\
            browser.Get("https://google.com").Wait();

            //Then
            var html = browser.GetSource();
        }
    }
}