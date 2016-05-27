using System;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using AutoHotkey.Interop;
using Gma.System.MouseKeyHook;
using PoePricer;
using ReactiveUI;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoePickitTestApp
{
    public class Program
    {
        private static TooltipWindow toolTipForm;

        private static Task uiTaskThread;

        private static WindowTracker poeWindowTracker;

        private static readonly ItemParse ItemParser = new ItemParse();

        [STAThread]
        private static void Main()
        {
            try
            {
                Console.WriteLine("Application has started");
                Console.WindowHeight = 40;
                Console.WindowWidth = 120;

                poeWindowTracker = new WindowTracker(() => "Path of Exile");

                InitializeAutohotkey();

                InitializeForm();
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

        private static void InitializeAutohotkey()
        {
            var ahkEngine = new AutoHotkeyEngine();
            var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"RemapPathOfExile.ahk");
            Console.WriteLine($"Loading AHK '{scriptPath}'...");
            ahkEngine.Load(scriptPath);
        }

        private static void InitializeForm()
        {
            toolTipForm = new TooltipWindow();
            var monitor = new PoeItemMonitor();

            var globalHook = Hook.GlobalEvents();

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => globalHook.KeyUp += h,
                    h => globalHook.KeyUp -= h)
                .Where(x => x.EventArgs.KeyData == Keys.LWin)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => toolTipForm.SetTooltip(null));

            monitor.PoeItemsSource
                .Where(_ => poeWindowTracker.IsActive)
                .DistinctUntilChanged(x => new { x, toolTipForm.Visibility })
                .Select(ItemParser.CreateTooltip)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(toolTipForm.SetTooltip);

            var app = new Application();
            app.Run(toolTipForm);
        }
    }
}