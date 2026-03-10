using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reactive.Linq;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.WinForms;
using PoeShared.Prism;
using PoeShared.UI.WinForms.Blazor;
using Unity;

namespace PoeShared.UI.WinForms;

public partial class Form1 : Form
{
    private readonly IUnityContainer container;
    private readonly IFactory<IBlazorWindow> blazorWindowFactory;
    private readonly BlazorContentHost blazorContentHost;
    private readonly List<ViewOption> viewOptions;
    private readonly ConcurrentDictionary<IBlazorWindow, byte> activeWindows = new();
    private MainCounterViewModel viewModel;
    private int contentVersion = 1;
    private int windowVersion = 1;

    public Form1(IUnityContainer container, IFactory<IBlazorWindow> blazorWindowFactory)
    {
        this.container = container;
        this.blazorWindowFactory = blazorWindowFactory;

        InitializeComponent();

        viewOptions = new()
        {
            new("Counter", typeof(MainCounterView)),
            new("Counter Alt", typeof(MainCounterViewAlt)),
            new("Slow", typeof(SlowView)),
            new("Broken", typeof(BrokenView))
        };

        viewTypeComboBox.DisplayMember = nameof(ViewOption.DisplayName);
        viewTypeComboBox.ValueMember = nameof(ViewOption.ComponentType);
        viewTypeComboBox.DataSource = viewOptions;

        viewModel = CreateViewModel();

        blazorContentHost = new BlazorContentHost
        {
            Dock = DockStyle.Fill,
            Container = container,
            ViewType = typeof(MainCounterView),
            Content = viewModel
        };

        hostPanel.Controls.Add(blazorContentHost);
        blazorContentHost.BringToFront();

        viewTypeComboBox.SelectedIndexChanged += (_, _) => ApplySelectedView();
        replaceContentButton.Click += (_, _) => ReplaceContent();
        incrementCountButton.Click += (_, _) => IncrementCount();
        reloadHostButton.Click += async (_, _) => await blazorContentHost.Reload();
        devToolsButton.Click += async (_, _) => await blazorContentHost.OpenDevTools();
        openWindowButton.Click += (_, _) => ShowWindow(isModal: false);
        openDialogWindowButton.Click += (_, _) => ShowWindow(isModal: true);
        closeAllWindowsButton.Click += (_, _) => CloseAllWindows();

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        FormClosed += OnFormClosed;

        viewTypeComboBox.SelectedIndex = 0;
        ApplySelectedView();
        UpdateStatusLabel();
    }

    private void ApplySelectedView()
    {
        if (viewTypeComboBox.SelectedItem is not ViewOption option)
        {
            return;
        }

        blazorContentHost.ViewType = option.ComponentType;
        UpdateStatusLabel();
    }

    private void ReplaceContent()
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.Dispose();

        viewModel = CreateViewModel();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        blazorContentHost.Content = viewModel;

        UpdateStatusLabel();
    }

    private void IncrementCount()
    {
        viewModel.Count++;
        UpdateStatusLabel();
    }

    private void ShowWindow(bool isModal)
    {
        if (viewTypeComboBox.SelectedItem is not ViewOption option)
        {
            return;
        }

        var window = blazorWindowFactory.Create();
        ConfigureWindow(window, option);
        TrackWindow(window);

        if (isModal)
        {
            _ = Task.Run(() => window.ShowDialog());
            return;
        }

        window.Show();
    }

    private void ConfigureWindow(IBlazorWindow window, ViewOption option)
    {
        var windowId = windowVersion++;
        window.Container = container;
        window.ViewType = option.ComponentType;
        window.DataContext = shareContentCheckBox.Checked ? viewModel : CreateWindowContent(option, windowId);
        window.Title = $"{option.DisplayName} Window #{windowId}";
        window.Width = 960;
        window.Height = 640;
    }

    private object CreateWindowContent(ViewOption option, int windowId)
    {
        return new MainCounterViewModel($"{option.DisplayName} window content #{windowId}");
    }

    private void TrackWindow(IBlazorWindow window)
    {
        activeWindows.TryAdd(window, 0);
        window
            .WhenClosed
            .Take(1)
            .Subscribe(_ => OnWindowClosed(window));
        UpdateStatusLabel();
    }

    private void OnWindowClosed(IBlazorWindow window)
    {
        activeWindows.TryRemove(window, out _);
        window.Dispose();

        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(UpdateStatusLabel);
            return;
        }

        UpdateStatusLabel();
    }

    private void CloseAllWindows()
    {
        foreach (var window in activeWindows.Keys.ToArray())
        {
            try
            {
                window.Close();
            }
            catch
            {
                window.Dispose();
            }
        }
    }

    private MainCounterViewModel CreateViewModel()
    {
        return new MainCounterViewModel($"Counter content #{contentVersion++}");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateStatusLabel);
            return;
        }

        UpdateStatusLabel();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        CloseAllWindows();
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.Dispose();
    }

    private void UpdateStatusLabel()
    {
        var selectedView = (viewTypeComboBox.SelectedItem as ViewOption)?.DisplayName ?? "<none>";
        statusLabel.Text =
            $"Host View: {selectedView} | Content: {viewModel.DisplayName} | Count: {viewModel.Count} | Instance: {viewModel.InstanceId.ToString("N")[..8]} | Windows: {activeWindows.Count}";
    }

    private sealed record ViewOption(string DisplayName, Type ComponentType);
}
