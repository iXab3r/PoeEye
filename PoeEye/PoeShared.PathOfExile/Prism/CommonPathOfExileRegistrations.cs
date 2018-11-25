using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using Gma.System.MouseKeyHook;
using PoeShared.Audio;
using PoeShared.Audio.Services;
using PoeShared.Audio.ViewModels;
using PoeShared.Common;
using PoeShared.Communications;
using PoeShared.Converters;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeDatabase;
using PoeShared.PoeDatabase.PoeNinja;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Resources.Notifications;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.StashApi;
using PoeShared.StashApi.DataTypes;
using PoeShared.StashApi.ProcurementLegacy;
using PoeShared.UI.Models;
using PoeShared.UI.ViewModels;
using ProxyProvider;
using ReactiveUI;
using TypeConverter;
using Unity;
using Unity.Extension;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace PoeShared.Prism
{
    public sealed class CommonPathOfExileRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IEqualityComparer<IPoeItem>, PoeItemEqualityComparer>()
                .RegisterSingleton<GearTypeAnalyzer>(typeof(IGearTypeAnalyzer), typeof(IItemTypeAnalyzer))
                .RegisterSingleton<IPoeLeagueApiClient, PoeLeagueApiClient>()
                .RegisterSingleton<PoeStashItemToPoeItemConverter>(typeof(IConverter<IStashItem, IPoeItem>), typeof(IConverter<IStashItem, PoeItem>))
                .RegisterSingleton<IConverter<string, PoePrice>>(new InjectionFactory(x => StringToPoePriceConverter.Instance));

            Container
                .RegisterType<IScheduler>(WellKnownSchedulers.UI, new InjectionFactory(x => RxApp.MainThreadScheduler))
                .RegisterType<IScheduler>(WellKnownSchedulers.Background,
                    new InjectionFactory(x => RxApp.TaskpoolScheduler))
                .RegisterType<IPoeLiveHistoryProvider, PoeLiveHistoryProvider>()
                .RegisterType<IPoeApiWrapper, PoeApiWrapper>()
                .RegisterType<IPageParameterDataViewModel, PageParameterDataViewModel>()
                .RegisterType<IPoeStashClient, PoeStashClient>()
                .RegisterType<IPoeStaticDataProvider, PoeStaticDataProvider>()
                .RegisterType<IPoeEconomicsSource, PoeNinjaDatabaseReader>();

            Container.RegisterWindowTracker(WellKnownWindows.PathOfExileWindow, () => "^Path of Exile$");
            Container
                .RegisterOverlayController(WellKnownOverlays.PathOfExileOverlay, WellKnownWindows.PathOfExileWindow);

            Container
                .RegisterType<IPoeDatabaseReader>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(
                        unity => unity.Resolve<ComplexPoeDatabaseReader>(
                            new DependencyOverride<IPoeDatabaseReader[]>(
                                new IPoeDatabaseReader[]
                                {
                                    unity.Resolve<StaticPoeDatabaseReader>(),
                                    unity.Resolve<PoeNinjaDatabaseReader>()
                                }
                            )
                        )));
        }
    }
}