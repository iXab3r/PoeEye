using ILog = Common.Logging.ILog;
using LogManager = Common.Logging.LogManager;

namespace PoeEye
{
    using System;

    using PoeTrade;

    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Program));

        private static void Main()
        {
            try
            {
                Log.InfoFormat("Application started");
                ResizeConsole();

                var poeTradeApi = new PoeTradeApi(new PoeTradeParserModern());

                poeTradeApi.IssueQuery(null).Subscribe(x => Log.Debug($"Result: {x.Raw}"));
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

        private static void ResizeConsole()
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