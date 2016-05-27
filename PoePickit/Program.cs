using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClipboardMonitor;
using Gma.System.MouseKeyHook;
using Microsoft.VisualBasic;



namespace PoePickit
{
    using Extensions;

    public class Program
    {
        private static ToolTipForm toolTipForm;

        private static Task uiTaskThread;

        private static WindowTracker poeWindowTracker;

        private static ItemParse itemParser = new ItemParse();

        private static string lastItemTextdata;

        private static Point lastTooltipCoor;
        

        private static void Main()
        {
            try
            {
                Console.WriteLine("Application has started");
                Console.WindowHeight = 60;
                Console.WindowWidth = 120;
                
                Console.WriteLine($"Samples to process: {StaticData.ItemSamples.Count()}");

                poeWindowTracker = new WindowTracker(() => "Path of Exile");

                uiTaskThread = Task.Factory.StartNew(InitializeForm);


               
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

        private static void InitializeForm()
        {
            toolTipForm = new ToolTipForm();
            var monitor = new PoeItemMonitor();
            var uiContext = SynchronizationContext.Current;
            
            var globalHook = Hook.GlobalEvents();

            
         


            globalHook.MouseMoveExt += OnMouseMove;
            //globalHook.KeyDown += OnKeyDown;
            //globalHook.KeyPress += OnKeyPress;
           // globalHook.KeyUp += GlobalHook_KeyUp;

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => globalHook.KeyUp += h,
                    h => globalHook.KeyUp -= h)
                .Where(x => x.EventArgs.KeyData == (Keys.LWin | Keys.RWin))
                .ObserveOn(uiContext)
                .Subscribe(_ => toolTipForm.Initialize(null));

            monitor.PoeItemsSource
                .Where(_ => poeWindowTracker.IsActive)
                .ObserveOn(uiContext)
                .Subscribe(OnNextPoeItemArrived);

            Application.Run(toolTipForm);
        }

        private static void GlobalHook_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.LWin)
            {
                
                
            }
        }

        private static void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.A)
            {
                
                
            }
        }

        
        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.CapsLock)
            {
             
                

            }
        }

        
        private static void OnMouseMove(object sender, MouseEventExtArgs e)
        {
            if (!toolTipForm.Visible)
                return;

            if (Math.Pow(Cursor.Position.X - lastTooltipCoor.X, 2) + Math.Pow(Cursor.Position.Y - lastTooltipCoor.Y, 2) >
                Math.Pow(45, 2))
                toolTipForm.Hide();
                
        }

        private static void OnNextPoeItemArrived(string itemData)
        {
            if (toolTipForm == null)
            {
                return;
            }
            if ((itemData == lastItemTextdata) && toolTipForm.Visible)
            {
                return;
            }
            var itemTooltip = itemParser.CreateTooltip(itemData);
            toolTipForm.Initialize(itemTooltip);
            lastTooltipCoor = Cursor.Position;
            lastItemTextdata = itemData;
        }
        
    }
}