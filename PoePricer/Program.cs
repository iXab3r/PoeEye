using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoePricer
{
    using Extensions;

    class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Application has started");

                Console.WindowHeight = 60;
                Console.WindowWidth = 120;

                Console.WriteLine($"Samples to process: {StaticData.ItemSamples.Count()}");

                var pricer = new PoePricer();

                foreach (var itemSample in StaticData.ItemSamples)
                {
                    try
                    {
                        Console.WriteLine("Processing next item...");
                        ConsoleExtensions.WriteLine(itemSample, ConsoleColor.Yellow);
                        var toolTip = pricer.CreateTooltip(itemSample);

                        Console.WriteLine("========== TOOLTIP STARTS HERE ===========");
                        ConsoleExtensions.WriteLine(toolTip, ConsoleColor.Green);
                        Console.WriteLine("========== TOOLTIP ENDS HERE   ===========");

                        Console.WriteLine($"Item was successfully processed\r\n\r\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse item, exception: {ex}\r\n\t{itemSample}");
                    }
                }

                Console.WriteLine("Press any key...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception: {ex}");
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}
