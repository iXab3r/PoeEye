using System;
using System.Windows;
using Microsoft.Practices.Unity;
using PoeEye.TradeSummary.Prism;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeEye.TradeSummary
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (!AppArguments.Parse(arguments))
            {
                Log.InitializeLogging("Startup");
                throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
            }

            if (!AppArguments.Instance.IsDebugMode)
            {
                Log.Instance.Error("TradeSummary must be started as a separate app only in debug mode");
                return;
            }
            Log.Instance.Warn("Running TradeSummary in debug mode");

            try
            {
                
                var container = new UnityContainer();

                container
                    .RegisterSingleton<IConfigProvider, PoeEyeConfigProviderInMemory>();

                var shared = new PoeSharedModule(container);
                shared.Initialize();
                
                var module = new PoeTradeSummaryModule(container);
                module.Initialize();
                
                new Application().Run();
            }
            catch (Exception e)
            {
                Log.Instance.Error(e);
            }
        }
    }
}
