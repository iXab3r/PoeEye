using System;
using System.Windows;
using Common.Logging;
using PoeEye.TradeMonitor.Prism;
using PoeShared;
using PoeShared.PoeControlPanel.Modularity;
using Unity;

namespace PoeEye.TradeMonitor
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        [STAThread]
        private static void Main()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (!AppArguments.Parse(arguments))
            {
                PoeShared.Log.InitializeLogging("Startup");
                throw new ApplicationException($"Failed to parse command line args: {string.Join(" ", arguments)}");
            }

            if (!AppArguments.Instance.IsDebugMode)
            {
                Log.Error("Oracle must be started as a separate app only in debug mode");
                return;
            }

            Log.Warn("Running Oracle in debug mode");


            var container = new UnityContainer();

            var shared = new PoeControlPanelModule(container);
            shared.OnInitialized(null);

            var module = new PoeTradeMonitorModule(container);
            module.OnInitialized(null);

            new Application().Run();
        }
    }
}