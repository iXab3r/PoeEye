using System;
using System.IO;
using System.Reflection;
using System.Threading;
using CefSharp.OffScreen;
using PoeShared;
using PoeShared.Communications.Chromium;
using PoeShared.Prism;

namespace PoeEye.Tests.PoeShared.Communications.Chromium
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    [Ignore("Chromium is currently not testably w/o hacks")]
    public class ChromiumBootstrapperFixture
    {
        private Mock<IFactory<ChromiumBrowser, ChromiumWebBrowser>> browserFactory;
        
        [SetUp]
        public void SetUp()
        {
            browserFactory = new Mock<IFactory<ChromiumBrowser, ChromiumWebBrowser>>();
        }
        
        [TestFixtureSetUp]
        public void SetUpFixture()
        {
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

        private ChromiumBootstrapper CreateInstance()
        {
            return new ChromiumBootstrapper(browserFactory.Object);
        }
    }
}