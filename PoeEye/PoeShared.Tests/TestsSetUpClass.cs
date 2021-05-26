using System.IO;
using NUnit.Framework;

namespace PoeShared.Tests
{
    [SetUpFixture]
    public class TestsSetUpClass
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SharedLog.Instance.LoadLogConfiguration(new FileInfo("log4net.tests.config"));
        }
    }
}