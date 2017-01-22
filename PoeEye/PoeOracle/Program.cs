using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Practices.Unity;
using PoeOracle.Prism;
using PoeShared.Prism;

namespace PoeOracle
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var container = new UnityContainer();
            container.AddExtension(new CommonRegistrations());

            var module = new PoeOracleModule(container);
            module.Initialize();

            new Application().Run();
        }
    }
}
