using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClipboardMonitor;
using Gma.System.MouseKeyHook;


namespace PoePickit
{
    using Extensions;

    public class Program
    {
        private static TtForm toolTipForm;

        private static Task uiTaskThread;

        private static WindowTracker poeWindowTracker;

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
            toolTipForm = new TtForm();
            var monitor = new PoeItemMonitor();
            var uiContext = SynchronizationContext.Current;

            var globalHook = Hook.GlobalEvents();
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => globalHook.KeyUp += h,
                    h => globalHook.KeyUp -= h)
                .Where(x => x.EventArgs.KeyData == (Keys.LControlKey | Keys.Control))
                .ObserveOn(uiContext)
                .Subscribe(_ => toolTipForm.Initialize(null));

            monitor.PoeItemsSource
                .Where(_ => poeWindowTracker.IsActive)
                .ObserveOn(uiContext)
                .Subscribe(OnNextPoeItemArrived);

            Application.Run(toolTipForm);
        }

        private static void OnNextPoeItemArrived(string itemData)
        {
            if (toolTipForm == null)
            {
                return;
            }
            var itemTooltip = new ItemParse().CreateTooltip(itemData);
            toolTipForm.Initialize(itemTooltip);
        }
    }
}