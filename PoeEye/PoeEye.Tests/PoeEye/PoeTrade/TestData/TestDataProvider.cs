using System.IO;

namespace PoeEye.Tests.PoeEye.PoeTrade.TestData
{
    internal sealed class TestDataProvider
    {
        public static string AncientWhiteResult => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.AncientWhite.Sample.xml");

        public static string ModernResult => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.Modern.Sample.xml");

        public static string ModernLiveResultItemGone => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.Modern.SampleN4.Live.ItemGone.xml");
        public static string ModernLiveResultNewItem => File.ReadAllText(@"PoeTrade\TestData\PoeTrade.Modern.SampleN4.Live.NewItem.xml");
    }
}