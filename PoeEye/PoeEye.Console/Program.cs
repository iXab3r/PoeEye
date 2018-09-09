using System;
using PoeEye.PoeTrade;
using PoeEye.PoeTrade.Prism;
using PoeEye.PoeTradeRealtimeApi;
using PoeEye.PoeTradeRealtimeApi.Prism;
using PoeShared;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Modularity;
using Unity;
using Unity.Resolution;

namespace PoeEye.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Log.Instance.Debug("Initializing API...");

                var container = new UnityContainer();
                container.AddExtension(new CommonRegistrations());

                var modules = new IModule[]
                {
                    new PoeTradeModule(container),
                    new PoeTradeRealtimeModule(container)
                };
                foreach (var module in modules)
                {
                    module.OnInitialized(null);
                }

                var api = container.Resolve<IPoeApi>($"{nameof(PoeEye)}.{nameof(PoeTrade)}.{nameof(PoeTradeApi)}");
                Log.Instance.Debug($"API: {api}");

                var query = new PoeQueryInfo
                {
                    League = "Prophecy",
                    AccountName = "Xab3r",
                    OnlineOnly = true,
                    NormalizeQuality = true
                };
                var source = container.Resolve<IRealtimeItemSource>(new DependencyOverride<IPoeQueryInfo>(query));
                var result = source.GetResult();

                Log.Instance.Info($"[Result] {result.ItemsList.Length.DumpToText()}");

                while (true)
                {
                    System.Console.WriteLine("Press any key...");
                    System.Console.ReadKey();

                    var liveResult = source.GetResult();
                    Log.Instance.Info($"[LiveResult] {liveResult.ItemsList.Length.DumpToText()}");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
            }
            finally
            {
                System.Console.WriteLine("Press any key...");
                System.Console.ReadKey();
            }
        }
    }
}