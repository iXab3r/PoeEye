using System;
using Common.Logging;
using PoeEye.PoeTrade;
using PoeEye.PoeTrade.Prism;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using Prism.Modularity;
using Unity;
using Unity.Resolution;

namespace PoeEye.Console
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static void Main(string[] args)
        {
            try
            {
                Log.Debug("Initializing API...");

                var container = new UnityContainer();
                container.AddExtension(new CommonRegistrations());

                var modules = new IModule[]
                {
                    new PoeTradeModule(container),
                };
                foreach (var module in modules)
                {
                    module.OnInitialized(null);
                }

                var api = container.Resolve<IPoeApi>($"{nameof(PoeEye)}.{nameof(PoeTrade)}.{nameof(PoeTradeApi)}");
                Log.Debug($"API: {api}");

                var query = new PoeQueryInfo
                {
                    League = "Prophecy",
                    AccountName = "Xab3r",
                    OnlineOnly = true,
                    NormalizeQuality = true
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                System.Console.WriteLine("Press any key...");
                System.Console.ReadKey();
            }
        }
    }
}