using PoeEye.StashRealtimeApi.API;
using PoeEye.StashRealtimeApi.Models;
using PoeEye.StashRealtimeApi.Services;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.StashApi.DataTypes;
using Unity;
using Unity.Extension;

namespace PoeEye.StashRealtimeApi.Prism
{
    internal sealed class LiveRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoeApi, PoeStashRealtimeApi>(typeof(PoeStashRealtimeApi).FullName);

            Container
                .RegisterType<IPoeItemsProcessor, PoeItemsProcessor>()
                .RegisterType<IFactory<IPoeItem, IStashItem, StashTab>, PoeItemFactory>()
                .RegisterType<IPoeItemsSource, PoeItemsSource>();
        }
    }
}