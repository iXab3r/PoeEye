using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using PoeShared.Audio.ViewModels;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding;
using PoeShared.RegionSelector;
using PoeShared.RegionSelector.Services;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Bindings;
using Size = System.Drawing.Size;

namespace PoeShared.UI;

internal sealed class MainWindowViewModel : DisposableReactiveObject
{
    public AutoCompleteSandboxViewModel AutoCompleteSandbox { get; }
    private readonly IScreenRegionSelectorService regionSelectorService;

    public MainWindowViewModel(
        IAudioNotificationSelectorViewModel audioNotificationSelector,
        IRandomPeriodSelector randomPeriodSelector,
        ISelectionAdornerViewModel selectionAdorner,
        IScreenRegionSelectorService regionSelectorService,
        NotificationSandboxViewModel notificationSandbox,
        ExceptionSandboxViewModel exceptionSandbox,
        IHotkeySequenceEditorViewModel hotkeySequenceEditor,
        AutoCompleteSandboxViewModel autoCompleteSandbox,
        BindingsSandboxViewModel bindingsSandbox)
    {
        AutoCompleteSandbox = autoCompleteSandbox;
        this.regionSelectorService = regionSelectorService;
        BindingsSandbox = bindingsSandbox.AddTo(Anchors);
        NotificationSandbox = notificationSandbox.AddTo(Anchors);
        ExceptionSandbox = exceptionSandbox.AddTo(Anchors);
        SelectionAdorner = selectionAdorner.AddTo(Anchors);
        AudioNotificationSelector = audioNotificationSelector.AddTo(Anchors);
        RandomPeriodSelector = randomPeriodSelector.AddTo(Anchors);
        HotkeySequenceEditor = hotkeySequenceEditor.AddTo(Anchors);
        LongCommand = CommandWrapper.Create(async () =>
        {
            await Task.Delay(3000);
        });
            
        ErrorCommand = CommandWrapper.Create(async () =>
        {
            await Task.Delay(3000);
            throw new ApplicationException("Error");
        });
            
        RandomPeriodSelector.LowerValue = TimeSpan.FromSeconds(3);
        RandomPeriodSelector.UpperValue = TimeSpan.FromSeconds(3);
        NextRandomPeriodCommand = CommandWrapper.Create(() => RandomPeriod = randomPeriodSelector.GetValue());
        StartSelectionCommand = CommandWrapper.Create(HandleSelectionCommandExecuted);
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
    }

    private async Task SelectRegionExecuted()
    {
        SelectedRegion = await regionSelectorService.SelectRegion(new Size(20, 20));
    }

    public NotificationSandboxViewModel NotificationSandbox { get; }
    public ExceptionSandboxViewModel ExceptionSandbox { get; }
    public BindingsSandboxViewModel BindingsSandbox { get; }

    public ICommand StartSelectionCommand { get; }


    public Rectangle SelectionRectangle { get; set; }

    public Rect SelectionRect { get; set; }

    public ISelectionAdornerViewModel SelectionAdorner { get; }

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

    private async Task HandleSelectionCommandExecuted()
    {
        SelectionRect = await SelectionAdorner.StartSelection().Take(1);
    }
}