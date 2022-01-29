using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Prism;
using Prism.Commands;
using Prism.Modularity;
using ReactiveUI;
using Unity;

namespace PoeShared.UI;

internal sealed class GenericSettingsViewModel : DisposableReactiveObject, IGenericSettingsViewModel
{
    private static readonly IFluentLog Log = typeof(GenericSettingsViewModel).PrepareLogger();

    private static readonly MethodInfo ReloadConfigMethod = typeof(GenericSettingsViewModel)
        .GetMethod(nameof(ReloadTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo SaveConfigMethod = typeof(GenericSettingsViewModel)
        .GetMethod(nameof(SaveTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IUnityContainer container;
    private readonly ConcurrentDictionary<Type, MethodInfo> reloadConfigByType = new ConcurrentDictionary<Type, MethodInfo>();
    private readonly ConcurrentDictionary<Type, MethodInfo> saveConfigByType = new ConcurrentDictionary<Type, MethodInfo>();

    private readonly ISourceList<ISettingsViewModel> moduleSettings = new SourceList<ISettingsViewModel>();
        
    public GenericSettingsViewModel(
        [NotNull] IPoeEyeModulesEnumerator modulesEnumerator,
        [NotNull] IUnityContainer container,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        Guard.ArgumentNotNull(modulesEnumerator, nameof(modulesEnumerator));
        Guard.ArgumentNotNull(container, nameof(container));
        this.container = container;

        modulesEnumerator
            .Settings
            .Connect()
            .StartWithDefault()
            .CombineLatest(this.WhenAnyValue(x => x.IsOpen)
                    .Where(x => x)
                    .Take(1),
                (unit, b) => Unit.Default)
            .Select(x => modulesEnumerator.Settings.Items.ToArray())
            .SubscribeSafe(ReloadModulesList, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.IsOpen)
            .WithPrevious((prev, curr) => new {prev, curr})
            .DistinctUntilChanged()
            .Where(x => x.curr && x.prev == false)
            .SubscribeSafe(ReloadConfigs, Log.HandleUiException)
            .AddTo(Anchors);

        SaveConfigCommand = new DelegateCommand(
            () =>
            {
                SaveConfigs();
                IsOpen = false;
            });

        CancelCommand = new DelegateCommand(() => IsOpen = false);

        moduleSettings
            .Connect()
            .ObserveOn(uiScheduler)
            .Bind(out var moduleSettingsSource)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        ModulesSettings = moduleSettingsSource;
    }

    public ReadOnlyObservableCollection<ISettingsViewModel> ModulesSettings { get; } 

    public bool IsOpen { get; set; }

    public ICommand SaveConfigCommand { get; }

    public ICommand CancelCommand { get; }

    private void ReloadModulesList(ISettingsViewModel[] viewModels)
    {
        moduleSettings.Clear();
        moduleSettings.AddRange(viewModels);
    }

    private void ReloadConfigs()
    {
        ModulesSettings.ForEach(ReloadConfig);
    }

    private void SaveConfigs()
    {
        ModulesSettings.ForEach(SaveConfig);
    }

    private Type GetConfigType(ISettingsViewModel viewModel)
    {
        var expectedInterface = typeof(ISettingsViewModel<>);
        var genericArgs = viewModel.GetType()
            .GetInterfaces()
            .Where(x => x.IsGenericType)
            .FirstOrDefault(x => expectedInterface.IsAssignableFrom(x.GetGenericTypeDefinition()));
        if (genericArgs == null)
        {
            throw new ModuleTypeLoadingException($"Failed to load settings of type {viewModel.GetType()} - interface {expectedInterface} was not found");
        }

        var configType = genericArgs.GetGenericArguments().First();
        return configType;
    }

    private void ReloadConfig(ISettingsViewModel viewModel)
    {
        var configType = GetConfigType(viewModel);
        Log.Debug(() => $"[PoeSettingsViewModel.ReloadConfig] Loading viewModel {viewModel} (configType {configType}");
        var invocationMethod = reloadConfigByType.GetOrAdd(configType, x => ReloadConfigMethod.MakeGenericMethod(x));
        invocationMethod.Invoke(this, new object[] {viewModel});
    }

    private void SaveConfig(ISettingsViewModel viewModel)
    {
        var configType = GetConfigType(viewModel);
        Log.Debug(() => $"[PoeSettingsViewModel.SaveConfig] Saving viewModel {viewModel} (configType {configType}");
        var invocationMethod = saveConfigByType.GetOrAdd(configType, x => SaveConfigMethod.MakeGenericMethod(x));
        invocationMethod.Invoke(this, new object[] {viewModel});
    }

    private void ReloadTypedConfig<TConfig>(ISettingsViewModel<TConfig> viewModel)
        where TConfig : class, IPoeEyeConfig, new()
    {
        var configProvider = container.Resolve<IConfigProvider<TConfig>>();
        viewModel.Load(configProvider.ActualConfig);
    }

    private void SaveTypedConfig<TConfig>(ISettingsViewModel<TConfig> viewModel)
        where TConfig : class, IPoeEyeConfig, new()
    {
        var configProvider = container.Resolve<IConfigProvider<TConfig>>();
        var config = viewModel.Save();
        configProvider.Save(config);
    }
}