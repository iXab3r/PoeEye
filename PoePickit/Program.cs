using System;
using System.Linq;
using System.Windows.Forms;




namespace PoePickit
{
    using Extensions;

    public class Program
    {
        private static void Main()
        {
            try
            {
                Console.WriteLine("Application has started");
                Console.WindowHeight = 60;
                Console.WindowWidth = 120;
                
                Console.WriteLine($"Samples to process: {StaticData.ItemSamples.Count()}");
                

                new TtHooks();
                //Console.ReadLine();


                //Thread ConsoleKeyListener = new Thread(new ThreadStart(ListerKeyBoardEvent(form)));
                //ConsoleKeyListener.Start();

                
                foreach (var itemSample in StaticData.ItemSamples)
                {
                    try
                    {
                        Console.WriteLine("Processing next item...");
                        ConsoleExtensions.WriteLine(itemSample, ConsoleColor.Yellow);

                        /*
                        var x = new Stopwatch();
                        Console.WriteLine($"--------------------------------");
                        Console.WriteLine($"StartItemParse: {x.ElapsedMilliseconds}");
                        Console.WriteLine($"--------------------------------");
                        x.Start();*/
                        

                        Console.WriteLine("========== TOOLTIP STARTS HERE ===========");
                        
                        
                        while (true)
                        {
                            Application.DoEvents();
                            
                        }

                        
                        /*Console.WriteLine($"--------------------------------");
                        Console.WriteLine($"StopItemParse: {x.ElapsedMilliseconds}");
                        Console.WriteLine($"--------------------------------");
                        x.Stop();*/

                        //ConsoleExtensions.WriteLine(toolTip, ConsoleColor.Green);

                        Console.WriteLine("========== TOOLTIP ENDS HERE   ===========");

                        /*Console.WriteLine($"Item was successfully processed\r\n\r\n");*/
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
