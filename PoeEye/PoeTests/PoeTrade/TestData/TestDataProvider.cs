namespace PoeEye.Tests.PoeTrade.TestData
{
    using System.IO;

    internal sealed class TestDataProvider
    {
        public static string AncientWhiteResult
            => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.AncientWhite.Sample.xml");

        public static string ModernResult => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.Modern.Sample.xml");
    }
}