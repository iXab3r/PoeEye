using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    [NotNull] private readonly IPoeEyeModulesEnumerator modulesEnumerator;
    private readonly IUnityContainer container;
    private readonly ConcurrentDictionary<Type, MethodInfo> reloadConfigByType = new();
    private readonly ConcurrentDictionary<Type, MethodInfo> saveConfigByType = new();

    private readonly ISourceList<ISettingsViewModel> moduleSettings = new SourceListEx<ISettingsViewModel>();
        
    public GenericSettingsViewModel(
        [NotNull] IPoeEyeModulesEnumerator modulesEnumerator,
        [NotNull] IUnityContainer container,
        [Dependency(WellKnownSchedulers.RedirectToUI)] IScheduler uiScheduler)
    {
        Guard.ArgumentNotNull(modulesEnumerator, nameof(modulesEnumerator));
        Guard.ArgumentNotNull(container, nameof(container));
        this.modulesEnumerator = modulesEnumerator;
        this.container = container;

        moduleSettings
            .Connect()
            .ObserveOn(uiScheduler)
            .BindToCollection(out var moduleSettingsSource)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        ModulesSettings = moduleSettingsSource;
        ReloadConfigs();
    }

    public IReadOnlyObservableCollection<ISettingsViewModel> ModulesSettings { get; }

    public void ReloadConfigs()
    {
        ReloadModulesList(modulesEnumerator.Settings.Items);
        moduleSettings.Items.ForEach(ReloadConfig);
    }

    public void SaveConfigs()
    {
        Log.Info($"Saving settings");
        moduleSettings.Items.ForEach(SaveConfig);
    }

    private void ReloadModulesList(IEnumerable<ISettingsViewModel> viewModels)
    {
        moduleSettings.EditDiff(viewModels);
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
        Log.Info(() => $"[PoeSettingsViewModel.ReloadConfig] Loading data into view model {viewModel} (configType {configType}");
        var invocationMethod = reloadConfigByType.GetOrAdd(configType, x => ReloadConfigMethod.MakeGenericMethod(x));
        invocationMethod.Invoke(this, new object[] {viewModel});
    }

    private void SaveConfig(ISettingsViewModel viewModel)
    {
        var configType = GetConfigType(viewModel);
        Log.Info(() => $"[PoeSettingsViewModel.SaveConfig] Saving view model {viewModel} (configType {configType}");
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