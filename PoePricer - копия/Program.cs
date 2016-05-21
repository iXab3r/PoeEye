using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security.Policy;
using Microsoft.VisualBasic.Devices;



namespace PoePricer
{
    using Extensions;

   

    public class Program
    {

        public static TtForm Forma = new TtForm();
        public bool ttActive = false;

        
        static void Main()
        {
            try
            {
                Console.WriteLine("Application has started");

                Console.WindowHeight = 60;
                Console.WindowWidth = 120;
                
                Console.WriteLine($"Samples to process: {StaticData.ItemSamples.Count()}");
                var pricer = new PoePricer();


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
                        pricer.CreateTooltip(itemSample);
                        pricer.ShowToolTip();
                        
                        while (true)
                        {
                            Application.DoEvents();
                            var cki = Console.ReadKey();
                            if (cki.Key == ConsoleKey.Spacebar)
                               break;
                             
                        }

                        pricer.HideToolTip();
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
