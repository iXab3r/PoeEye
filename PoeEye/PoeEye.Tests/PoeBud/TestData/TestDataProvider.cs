using System.IO;
using System.Linq;
using Moq;
using PoeBud.Models;
using PoeShared.StashApi.DataTypes;
using RestSharp;
using RestSharp.Deserializers;

namespace PoeEye.Tests.PoeBud.TestData
{
    internal sealed class TestDataProvider
    {
        public static StashUpdate Stash1_WithTabs => ParseUpdate(File.ReadAllText(@"PoeBud\TestData\Stash1_WithTabs.json"));
        public static StashUpdate Stash1_WithoutTabs => ParseUpdate(File.ReadAllText(@"PoeBud\TestData\Stash1_WithoutTabs.json"));

        public static StashUpdate ParseUpdate(string json)
        {
            var deserializer = new JsonDeserializer();

            var responseMock = Mock.Of<IRestResponse>(x => x.Content == json);
            var stash = deserializer.Deserialize<Stash>(responseMock);

            return new StashUpdate(stash.Items.OfType<IStashItem>().ToArray(), stash.Tabs.OfType<IStashTab>().ToArray());
        }
    }
}