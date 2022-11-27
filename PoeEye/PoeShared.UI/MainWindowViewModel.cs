using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EyeAuras.OnTopReplica;
using PoeShared.Audio.ViewModels;
using PoeShared.Blazor;
using PoeShared.Prism;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding;
using PoeShared.RegionSelector;
using PoeShared.RegionSelector.Services;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Bindings;
using PoeShared.UI.Blazor;
using PropertyBinder;
using Unity;
using Size = System.Drawing.Size;

namespace PoeShared.UI;

internal sealed class MainWindowViewModel : DisposableReactiveObject
{
    private static readonly Binder<MainWindowViewModel> Binder = new();
    private readonly IScreenRegionSelectorService regionSelectorService;

    static MainWindowViewModel()
    {
        Binder.Bind(x => x.ProjectionBounds.Bounds).To(x => x.SelectionAdorner.ProjectionBounds);
        Binder.Bind(x => x.SelectionAdorner.ProjectionBounds).To(x => x.ProjectionBounds.Bounds);
        
        Binder.Bind(x => x.SelectionProjected.Bounds).To(x => x.SelectionAdorner.SelectionProjected);
        Binder.Bind(x => x.SelectionAdorner.SelectionProjected).To(x => x.SelectionProjected.Bounds);
    }

    public MainWindowViewModel(
        IFactory<BlazorSandboxViewModel> blazorHostViewModelFactory,
        IAudioNotificationSelectorViewModel audioNotificationSelector,
        IRandomPeriodSelector randomPeriodSelector,
        SelectionAdorner selectionAdorner,
        IScreenRegionSelectorService regionSelectorService,
        NotificationSandboxViewModel notificationSandbox,
        ExceptionSandboxViewModel exceptionSandbox,
        IHotkeySequenceEditorViewModel hotkeySequenceEditor,
        AutoCompleteSandboxViewModel autoCompleteSandbox,
        BindingsSandboxViewModel bindingsSandbox)
    {
        SelectionAdorner = selectionAdorner.AddTo(Anchors);
        AutoCompleteSandbox = autoCompleteSandbox;
        this.regionSelectorService = regionSelectorService;
        BindingsSandbox = bindingsSandbox.AddTo(Anchors);
        NotificationSandbox = notificationSandbox.AddTo(Anchors);
        ExceptionSandbox = exceptionSandbox.AddTo(Anchors);
        AudioNotificationSelector = audioNotificationSelector.AddTo(Anchors);
        RandomPeriodSelector = randomPeriodSelector.AddTo(Anchors);
        HotkeySequenceEditor = hotkeySequenceEditor.AddTo(Anchors);
        LongCommand = CommandWrapper.Create(async () => { await Task.Delay(3000); });

        ErrorCommand = CommandWrapper.Create(async () =>
        {
            await Task.Delay(3000);
            throw new ApplicationException("Error");
        });

        RandomPeriodSelector.LowerValue = TimeSpan.FromSeconds(3);
        RandomPeriodSelector.UpperValue = TimeSpan.FromSeconds(3);
        NextRandomPeriodCommand = CommandWrapper.Create(() => RandomPeriod = randomPeriodSelector.GetValue());
        StartSelectionBoxCommand = CommandWrapper.Create(async () =>
        {
            SelectionRectangle = await SelectionAdorner.StartSelection(supportBoxSelection: true).Take(1);
        });
        StartSelectionPointCommand = CommandWrapper.Create(async () =>
        {
            SelectionRectangle = await SelectionAdorner.StartSelection(supportBoxSelection: false).Take(1);
        });
        StartSelectionPointStreamCommand = CommandWrapper.Create(async () =>
        {
            await SelectionAdorner.StartSelection(supportBoxSelection: false)
                .Do(x => SelectionRectangle = x);
        });
        SetCachedControlContentCommand = CommandWrapper.Create<object>(arg =>
        {
            if (arg is string name)
            {
                FakeDelay = new FakeDelayStringViewModel() { Name = name };
            }
            else if (arg is int num)
            {
                FakeDelay = new FakeDelayNumberViewModel() { Number = num };
            }
            else
            {
                FakeDelay = null;
            }
        });

        SelectRegionCommnad = CommandWrapper.Create(SelectRegionExecuted);
        BlazorSandbox = blazorHostViewModelFactory.Create();
        Binder.Attach(this).AddTo(Anchors);
    }

    public SelectionAdorner SelectionAdorner { get; }
    public AutoCompleteSandboxViewModel AutoCompleteSandbox { get; }

    public NotificationSandboxViewModel NotificationSandbox { get; }
    public ExceptionSandboxViewModel ExceptionSandbox { get; }
    public BindingsSandboxViewModel BindingsSandbox { get; }
    public BlazorSandboxViewModel BlazorSandbox { get; }
    public ICommand StartSelectionBoxCommand { get; }
    public ICommand StartSelectionPointCommand { get; }
    public ICommand StartSelectionPointStreamCommand { get; }

    public Color Color { get; set; }

    public System.Drawing.Rectangle SelectionRectangle { get; set; }

    public ReactiveRectangle SelectionProjected { get; } = new();
    public ReactiveRectangle ProjectionBounds { get; } = new();

    public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

    public IRandomPeriodSelector RandomPeriodSelector { get; }

    public Fallback<string> FallbackValue { get; } = new Fallback<string>(string.IsNullOrWhiteSpace);

    public IHotkeySequenceEditorViewModel HotkeySequenceEditor { get; }

    public CommandWrapper LongCommand { get; }

    public CommandWrapper ErrorCommand { get; }
    
    public ICommand NextRandomPeriodCommand { get; }

    public ICommand SetCachedControlContentCommand { get; }

    public ICommand SelectRegionCommnad { get; }

    public RegionSelectorResult SelectedRegion { get; private set; }

    public DisposableReactiveObject FakeDelay { get; set; }

    public TimeSpan RandomPeriod { get; set; }

    public CommandWrapper ShowBlazorWindow { get; }

    private async Task SelectRegionExecuted()
    {
        SelectedRegion = await regionSelectorService.SelectRegion(new Size(20, 20));
    }
}