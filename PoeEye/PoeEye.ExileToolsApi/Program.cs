using System;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;

namespace PoeEye.ExileToolsApi
{
    public static class Program
    {
        public static void Main()
        {
            try
            {
                var api = new ExileToolsApi();

                api.RequestStaticData().Result.DumpToText();

                api.IssueQuery(new PoeQuery());
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}