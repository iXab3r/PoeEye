using System.Collections.Concurrent;
using PoeEye.PoeTrade.Models;
using PoeShared.PoeTrade;
using Prism.Commands;
using ReactiveUI.Legacy;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeModsEditorViewModel : DisposableReactiveObject, IPoeModsEditorViewModel
    {
        private readonly DelegateCommand addModCommand;

        private readonly IFactory<IPoeModViewModel> modsViewModelsFactory;
        private readonly DelegateCommand<IPoeModViewModel> removeModCommand;

        private PoeQueryModsGroupType groupType;

        private float? maxGroupValue;

        private float? minGroupValue;

        private readonly IReactiveSuggestionProvider suggestionProvider;
        private IDictionary<string, IPoeItemMod> knownModsByName = new Dictionary<string, IPoeItemMod>();

        public PoeModsEditorViewModel(
            [NotNull] IPoeStaticDataSource staticDataSource,
            [NotNull] IFactory<IPoeModViewModel> modsViewModelsFactory,
            [NotNull] IFactory<IReactiveSuggestionProvider> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(modsViewModelsFactory, nameof(modsViewModelsFactory));
            Guard.ArgumentNotNull(staticDataSource, nameof(staticDataSource));
            Guard.ArgumentNotNull(suggestionProviderFactory, nameof(suggestionProviderFactory));

            this.modsViewModelsFactory = modsViewModelsFactory;

            removeModCommand = new DelegateCommand<IPoeModViewModel>(RemoveModCommandExecuted);
            addModCommand = new DelegateCommand(() => AddMod());
            suggestionProvider = suggestionProviderFactory.Create();

            Observable.Merge(
                    Mods.Changed.ToUnit(),
                    Mods.ItemChanged.ToUnit())
                .Subscribe(() => removeModCommand.RaiseCanExecuteChanged()).AddTo(Anchors);
            
            staticDataSource
                .WhenAnyValue(x => x.StaticData)
                .Subscribe(staticData => KnownModsByName = ToItemMods(staticData))
                .AddTo(Anchors);

            this
                .WhenAnyValue(x => x.KnownModsByName)
                .Subscribe(modsByName => suggestionProvider.Items = KnownModsByName.Keys)
                .AddTo(Anchors);
            
            AddMod();
        }

        public ICommand AddModCommand => addModCommand;

        public ICommand RemoveModCommand => removeModCommand;

        public PoeQueryModsGroupType GroupType
        {
            get { return groupType; }
            set { this.RaiseAndSetIfChanged(ref groupType, value); }
        }

        public float? MinGroupValue
        {
            get { return minGroupValue; }
            set { this.RaiseAndSetIfChanged(ref minGroupValue, value); }
        }

        public float? MaxGroupValue
        {
            get { return maxGroupValue; }
            set { this.RaiseAndSetIfChanged(ref maxGroupValue, value); }
        }

        private IDictionary<string, IPoeItemMod> KnownModsByName
        {
            get { return knownModsByName; }
            set { this.RaiseAndSetIfChanged(ref knownModsByName, value); }
        }

        public IReactiveList<IPoeModViewModel> Mods { get; } = new ReactiveList<IPoeModViewModel> {ChangeTrackingEnabled = true};

        public IPoeModViewModel AddMod()
        {
            var newMod = modsViewModelsFactory.Create();
            newMod.SuggestionProvider = suggestionProvider;

            Mods.Add(newMod);

            return newMod;
        }

        public IPoeQueryModsGroup ToGroup()
        {
            var group = new PoeQueryModsGroup
            {
                Mods = ToMods(),
                GroupType = GroupType
            };

            if (GroupType == PoeQueryModsGroupType.Count || GroupType == PoeQueryModsGroupType.Sum)
            {
                group.Min = minGroupValue;
                group.Max = maxGroupValue;
            }

            return group;
        }

        private IDictionary<string, IPoeItemMod> ToItemMods(IPoeStaticData staticData)
        {
            var modsByName = staticData
                .ModsList
                .ToDictionary(x => x.Name, x => x);

            return modsByName;
        }

        private void RemoveModCommandExecuted(IPoeModViewModel modToRemove)
        {
            Guard.ArgumentNotNull(modToRemove, nameof(modToRemove));

            if (Mods.Count == 1)
            {
                modToRemove.Reset();
            }
            else
            {
                Mods.Remove(modToRemove);
            }
        }

        private IPoeQueryRangeModArgument[] ToMods()
        {
            var result = new List<IPoeQueryRangeModArgument>();
            foreach (var modModel in Mods)
            {
                if (string.IsNullOrWhiteSpace(modModel.SelectedMod))
                {
                    continue;
                }

                IPoeItemMod knownMod;
                if (!KnownModsByName.TryGetValue(modModel.SelectedMod, out knownMod))
                {
                    knownMod = new PoeItemMod() { CodeName = modModel.SelectedMod, Name = modModel.SelectedMod };
                }

                var modArg = new PoeQueryRangeModArgument(knownMod)
                {
                    Min = modModel.Min,
                    Max = modModel.Max
                };

                result.Add(modArg);
            }
            return result.ToArray();
        }
    }
}
