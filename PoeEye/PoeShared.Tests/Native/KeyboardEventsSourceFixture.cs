using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using WindowsInput;
using WindowsInput.Native;
using WindowsHook;
using log4net;
using Moq;
using NUnit.Framework;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using Shouldly;

namespace PoeShared.Tests.Native;

[TestFixture]
public class KeyboardEventsSourceFixture
{
    private static readonly IFluentLog Log = typeof(KeyboardEventsSourceFixture).PrepareLogger();

    private Mock<IClock> clock;
    private KeyboardMouseEventsProvider eventsFactory;
    private IScheduler inputScheduler;

    [SetUp]
    public void SetUp()
    {
        inputScheduler = Scheduler.Immediate;
        clock = new Mock<IClock>();
        eventsFactory = new KeyboardMouseEventsProvider(inputScheduler);
    }

    [Test]
    [Ignore("Not completed yet")]
    public async Task ShouldThrowWhenMultipleHooks()
    {
        //Given
        var wrappers = new ConcurrentBag<HookWrapper>();
        var startEvent = new ManualResetEvent(false);
        var hooksCount = 1;
        var wrappersCreated = Enumerable.Range(0, hooksCount).Select(x => new ManualResetEvent(false)).ToArray();
        var wrappersReady = Enumerable.Range(0, hooksCount).Select(x => new ManualResetEvent(false)).ToArray();
        var hookFormReady = new ManualResetEvent(false);
        HookForm hookForm;
            
        var bgThread = new Thread(x =>
        {
            try
            {
                Log.Debug(() => $"Creating form for hooking keyboard and mouse events");
                hookForm = new HookForm();
                Log.Debug(() => $"Running message loop in hook form");
                hookForm.Loaded += delegate
                {
                    Log.Debug(() => $"Hook form is loaded");
                    hookFormReady.Set();
                };
                var result = hookForm.ShowDialog();
                Log.Debug(() => $"Message loop terminated gracefully, dialog result: {result}");
            }
            catch (Exception e)
            {
                Log.HandleUiException(new ApplicationException("Exception occurred in Complex Hotkey message loop", e));
            }
            finally
            {
                Log.Debug(() => $"Hook form thread terminated");
            }
                
        })
        {
            IsBackground = true, 
            ApartmentState = ApartmentState.STA, 
            Name = "HotkeyTracker"
        };
        bgThread.Start();

        Task.Run(() =>
        {
            Log.Debug("Awaiting for hook form to be ready");
            hookFormReady.WaitOne();

            Enumerable.Range(0, hooksCount).AsParallel()
                .WithDegreeOfParallelism(hooksCount)
                .ForAll(async idx =>
                {
                    var wrapper = new HookWrapper();
                    wrappers.Add(wrapper);
                    Log.Debug(() => $"Created {wrapper}");
                    wrappersCreated[idx].Set();
                    startEvent.WaitOne();
                    Log.Debug(() => $"Starting {wrapper}");
                    wrapper.Start();
                    wrappersReady[idx].Set();
                });
                
        });
            
        Log.Debug("Awaiting for hook form to be ready");
        hookFormReady.WaitOne();

        Log.Debug("Awaiting for all wrappers to be created");
        WaitHandle.WaitAll(wrappersCreated);
           
        //When
        Log.Debug("GCing");
        GC.Collect();
        var simulator = new InputSimulator();
        Log.Debug("Kicking off wrappers");
        startEvent.Set();
        Log.Debug("Awaiting for all wrappers to be ready");
        WaitHandle.WaitAll(wrappersReady);
            
        Log.Debug("Sending key event");
        Thread.Sleep(3000);
        //simulator.Keyboard.KeyPress(VirtualKeyCode.F1);
        Thread.Sleep(3000);

        //Then
        wrappers.All(x => x.Events.Count > 0).ShouldBe(true);
        wrappers.All(x => x.Events.Count == 1).ShouldBe(true);
    }
        
    private sealed class HookForm : Window
    {
        private readonly CompositeDisposable anchors = new CompositeDisposable();

        public HookForm()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            Title = $"{assembly.GetName().Name} {assembly.GetName().Version} {nameof(HookForm)}";
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Width = 0;
            Height = 0;
            this.Loaded += OnLoaded;
            Log.Info("HookForm created");

            this.LogWndProc("HookForm").AddTo(anchors);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;

            Log.Info("HookForm loaded, applying style...");
            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            Log.Debug(() => $"HookForm handle: {hwnd.ToHexadecimal()}");
            UnsafeNative.HideSystemMenu(hwnd);
            UnsafeNative.SetWindowExTransparent(hwnd);
            UnsafeNative.SetWindowRgn(hwnd, Rectangle.Empty);
            Log.Info("HookForm successfully initialized");
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Info("HookForm closed");
            base.OnClosed(e);
            anchors.Dispose();
        }
    }

    private sealed class HookWrapper : DisposableReactiveObject
    {
        private static long GlobalHookId;

        public long HookId { get; }

        public ConcurrentQueue<EventArgs> Events { get; } = new();

        public HookWrapper()
        {
            HookId = Interlocked.Increment(ref GlobalHookId);
        }

        public void Start()
        {
            var hook = Hook.CreateGlobalEvents().AddTo(Anchors);
            hook.KeyDown += Hook2OnKeyDown;
        }

        private void Hook2OnKeyDown(object? sender, KeyEventArgs e)
        {
            Log.Debug(() => $"[{HookId}] Key down: {e.KeyCode}");
            Events.Enqueue(e);
        }

        public override string ToString()
        {
            return $"Hook {HookId}";
        }
    }

    private KeyboardEventsSource CreateInstance()
    {
        return new KeyboardEventsSource(eventsFactory, clock.Object, Scheduler.Immediate);
    }
}