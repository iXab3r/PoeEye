using System.IO;

namespace PoeEye.Tests.PoeTrade.TestData
{
    internal sealed class TestDataProvider
    {
        public string AncientWhiteResult
        {
            get { return File.ReadAllText(@"PoeTrade.AncientWhite.Sample.xml"); }
        } 
    }
}