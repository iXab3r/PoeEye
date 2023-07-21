using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Prism;
using Prism.Commands;
using Prism.Modularity;
using ReactiveUI;
using Unity;

namespace PoeShared.UI;

internal sealed class GenericSettingsViewModel : WindowViewModelBase, IGenericSettingsViewModel
{
    private static readonly MethodInfo ReloadConfigMethod = typeof(GenericSettingsViewModel)
        .GetMethod(nameof(ReloadTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo SaveConfigMethod = typeof(GenericSettingsViewModel)
        .GetMethod(nameof(SaveTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IPoeEyeModulesEnumerator modulesEnumerator;
    private readonly IUnityContainer container;
    private readonly ConcurrentDictionary<Type, MethodInfo> reloadConfigByType = new();
    private readonly ConcurrentDictionary<Type, MethodInfo> saveConfigByType = new();

    private readonly ISourceCache<ISettingsViewModel, Type> moduleSettings = new SourceCache<ISettingsViewModel, Type>(x => x.GetType());
        
    public GenericSettingsViewModel(
        [NotNull] IPoeEyeModulesEnumerator modulesEnumerator,
        [NotNull] IUnityContainer container,
        [Dependency(WellKnownSchedulers.RedirectToUI)] IScheduler uiScheduler)
    {
        Guard.ArgumentNotNull(modulesEnumerator, nameof(modulesEnumerator));
        Guard.ArgumentNotNull(container, nameof(container));
        this.modulesEnumerator = modulesEnumerator;
        this.container = container;
        DefaultSize = new WinSize(600, 600);
        MinSize = new WinSize(600, 200);
        Title = "SETTINGS";
        CloseCommand = CommandWrapper.Create(Close);
        SaveCommand = CommandWrapper.Create(SaveExecuted);

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
    
    public ICloseController CloseController { get; set; }
    
    public ICommand CloseCommand { get; }
    
    public ICommand SaveCommand { get; }

    public void ReloadConfigs()
    {
        ReloadModulesList(modulesEnumerator.Settings.Items);
        moduleSettings.Items.ForEach(ReloadConfig);
    }

    public void SaveConfigs()
    {
        Log.Info(() => $"Saving settings");
        moduleSettings.Items.ForEach(SaveConfig);
    }

    private void ReloadModulesList(IEnumerable<Type> types)
    {
        var toRemove = moduleSettings.Items.Select(x => x.GetType()).Except(types).ToArray();
        var toAdd = types.Except( moduleSettings.Items.Select(x => x.GetType())).ToArray();
        moduleSettings.RemoveKeys(toRemove);
        foreach (var type in toAdd)
        {
            var viewModel = (ISettingsViewModel)container.Resolve(type);
            moduleSettings.AddOrUpdate(viewModel);
        }
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

    public void Close()
    {
        this.CloseController?.Close();
    }
    
    public async Task SaveExecuted()
    {
        await Task.Run(SaveConfigs);
        Close();
    }
}