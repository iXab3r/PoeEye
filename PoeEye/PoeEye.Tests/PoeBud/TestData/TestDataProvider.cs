using System.IO;
using System.Linq;
using Moq;
using PoeBud.Models;
using PoeShared.StashApi.DataTypes;
using RestSharp;

namespace PoeEye.Tests.PoeBud.TestData
{
    internal sealed class TestDataProvider
    {
        public static StashUpdate Stash1 => ParseUpdate(File.ReadAllText(@"PoeBud\TestData\Stash1.json"));

        public static StashUpdate ParseUpdate(string json)
        {
            var deserializer = new RestSharp.Deserializers.JsonDeserializer();

            var responseMock = Mock.Of<IRestResponse>(x => x.Content == json);
            var stash = deserializer.Deserialize<Stash>(responseMock);

            return new StashUpdate(stash.Items.OfType<IStashItem>().ToArray(), stash.Tabs.OfType<IStashTab>().ToArray());
        }
    }
}