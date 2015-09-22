using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using PoeEye.PoeTrade;

namespace PoeEye
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        static void Main()
        {
            

            try
            {
                Log.InfoFormat("Application started");
                ResizeConsole();

                var poeTradeApi = new PoeTradeApi();

                poeTradeApi.IssueQuery(null).Subscribe(x => Log.DebugFormat("Result: {0}", x.Raw));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static void ResizeConsole()
        {
            try
            {
                Console.WindowHeight = 50;
                Console.WindowWidth = 160;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to resize Console window", ex);

            }
        }
    }
}
