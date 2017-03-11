using System;
using System.Windows;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Prism;
using PoeShared.Prism;

namespace PoeEye.TradeMonitor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var container = new UnityContainer();
            container.AddExtension(new CommonRegistrations());

            var module = new PoeTradeMonitorModule(container);
            module.Initialize();

            new Application().Run();
        }
    }
}
