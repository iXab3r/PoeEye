using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows.Input;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.Config;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Commands;
using Prism.Modularity;
using ReactiveUI;
using Unity;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeEyeSettingsViewModel : DisposableReactiveObject
    {
        private static readonly MethodInfo ReloadConfigMethod = typeof(PoeEyeSettingsViewModel)
                .GetMethod(nameof(ReloadTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SaveConfigMethod = typeof(PoeEyeSettingsViewModel)
                .GetMethod(nameof(SaveTypedConfig), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly IUnityContainer container;
        private readonly ConcurrentDictionary<Type, MethodInfo> reloadConfigByType = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> saveConfigByType = new ConcurrentDictionary<Type, MethodInfo>();

        private bool isOpen;

        public PoeEyeSettingsViewModel(
            [NotNull] IPoeEyeModulesEnumerator modulesEnumerator,
            [NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(modulesEnumerator, nameof(modulesEnumerator));
            Guard.ArgumentNotNull(container, nameof(container));
            this.container = container;

            Observable.CombineLatest(
                    modulesEnumerator
                        .Settings
                        .ToObservableChangeSet()
                        .ToUnit()
                        .StartWith(Unit.Default),
                    this.WhenAnyValue(x => x.IsOpen)
                        .Where(x => x)
                        .Take(1),
                    (unit, b) => Unit.Default)
                .Select(x => modulesEnumerator.Settings.ToList())
                .Subscribe(ReloadModulesList)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsOpen)
                .WithPrevious((prev, curr) => new { prev, curr })
                .DistinctUntilChanged()
                .Where(x => x.curr == true && x.prev == false)
                .Subscribe(ReloadConfigs)
                .AddTo(Anchors);

            SaveConfigCommand = new DelegateCommand(
                () =>
                {
                    SaveConfigs();
                    IsOpen = false;
                });

            CancelCommand = new DelegateCommand(() => IsOpen = false);
        }

        public IReactiveList<ISettingsViewModel> ModulesSettings { get; } = new ReactiveList<ISettingsViewModel>();

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public ICommand SaveConfigCommand { get; }
        
        public ICommand CancelCommand { get; }
        
        private void ReloadModulesList(IEnumerable<ISettingsViewModel> viewModels)
        {
            ModulesSettings.Clear();
            ModulesSettings.AddRange(viewModels);
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
            Log.Instance.Debug($"[PoeSettingsViewModel.ReloadConfig] Loading viewModel {viewModel} (configType {configType}");
            var invocationMethod = reloadConfigByType.GetOrAdd(configType, x => ReloadConfigMethod.MakeGenericMethod(x));
            invocationMethod.Invoke(this, new object[] { viewModel });
        }

        private void SaveConfig(ISettingsViewModel viewModel)
        {
            var configType = GetConfigType(viewModel);
            Log.Instance.Debug($"[PoeSettingsViewModel.SaveConfig] Saving viewModel {viewModel} (configType {configType}");
            var invocationMethod = saveConfigByType.GetOrAdd(configType, x => SaveConfigMethod.MakeGenericMethod(x));
            invocationMethod.Invoke(this, new object[] { viewModel });
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
}
