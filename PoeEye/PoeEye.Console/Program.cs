namespace PoeEye.Console
{
    using System;
    using System.Linq;

    using ConsoleDump;

    using DumpToText;

    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.Utility;

    using PoeShared;
    using PoeShared.PoeTrade;
    using PoeShared.Prism;

    using PoeTrade;

    using Prism;

    using Simulator.Prism;

    using Guard = Guards.Guard;

    internal class Program
    {
        private static readonly Options options = new Options();

        private static void Main(string[] args)
        {
            try
            {
                Log.Instance.InfoFormat("Application started");
                ResizeConsole();

                var parsedOptions = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
                Guard.ArgumentIsTrue(() => parsedOptions);

                var mainUnityBlock = options.Mode == Options.ProgramMode.Mock
                    ? (UnityContainerExtension) new MockRegistrations()
                    : (UnityContainerExtension) new LiveRegistrations();

                Log.Instance.Debug($"Unity main block: {mainUnityBlock.DumpToText()}");

                var unityContainer = new UnityContainer()
                    .AddExtension(new CommonRegistrations())
                    .AddExtension(mainUnityBlock);

                var poeApi = unityContainer.Resolve<IPoeApi>();

                poeApi
                    .IssueQuery(new PoeQuery())
                    .Subscribe(DumpQueryResults);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(ex);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static void DumpQueryResults(IPoeQueryResult queryResult)
        {
            Log.Instance.Debug(queryResult.DumpToText());

            var items =
                queryResult.ItemsList
                            .Where(x => !string.IsNullOrWhiteSpace(x.Price))
                           .Select(x => new {x.Price, x.ItemName, x.UserIgn})
                           .ToArray();
            items.Dump();
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
                Log.Instance.Error("Failed to resize Console window", ex);
            }
        }
    }
}