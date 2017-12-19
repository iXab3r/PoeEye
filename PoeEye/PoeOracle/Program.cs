using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using PoeEye;
using PoeOracle.Prism;
using PoeShared;
using PoeShared.Prism;

namespace PoeOracle
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
                Log.Instance.Error("Oracle must be started as a separate app only in debug mode");
                return;
            }
            Log.Instance.Warn("Running Oracle in debug mode");

            var container = new UnityContainer();
            container.AddExtension(new CommonRegistrations());

            var module = new PoeOracleModule(container);
            module.Initialize();

            new Application().Run();
        }
    }
}
