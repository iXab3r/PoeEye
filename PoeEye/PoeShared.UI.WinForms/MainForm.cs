using System.ComponentModel;
using PoeShared.Blazor.WinForms;
using PoeShared.UI.WinForms.Blazor;
using Unity;

namespace PoeShared.UI.WinForms;

public partial class Form1 : Form
{
    private readonly BlazorContentHost blazorContentHost;
    private readonly List<ViewOption> viewOptions;
    private MainCounterViewModel viewModel;
    private int contentVersion = 1;

    public Form1(IUnityContainer container)
    {
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

    private MainCounterViewModel CreateViewModel()
    {
        return new MainCounterViewModel($"Counter content #{contentVersion++}");
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(UpdateStatusLabel));
            return;
        }

        UpdateStatusLabel();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.Dispose();
    }

    private void UpdateStatusLabel()
    {
        var selectedView = (viewTypeComboBox.SelectedItem as ViewOption)?.DisplayName ?? "<none>";
        statusLabel.Text = $"View: {selectedView} | Content: {viewModel.DisplayName} | Count: {viewModel.Count} | Instance: {viewModel.InstanceId.ToString("N")[..8]}";
    }

    private sealed record ViewOption(string DisplayName, Type ComponentType);
}
