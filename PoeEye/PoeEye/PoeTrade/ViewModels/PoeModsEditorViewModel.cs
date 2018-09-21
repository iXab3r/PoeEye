using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeEye.PoeTrade.Models;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeModsEditorViewModel : DisposableReactiveObject, IPoeModsEditorViewModel
    {
        private readonly DelegateCommand addModCommand;

        private readonly IFactory<IPoeModViewModel> modsViewModelsFactory;
        private readonly DelegateCommand<IPoeModViewModel> removeModCommand;

        private readonly IReactiveSuggestionProvider suggestionProvider;

        private PoeQueryModsGroupType groupType = PoeQueryModsGroupType.And;
        private IDictionary<string, IPoeItemMod> knownModsByName = new Dictionary<string, IPoeItemMod>();

        private float? maxGroupValue;

        private float? minGroupValue;

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

            Mods.Changed.ToUnit().Merge(Mods.ItemChanged.ToUnit())
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

        private IDictionary<string, IPoeItemMod> KnownModsByName
        {
            get => knownModsByName;
            set => this.RaiseAndSetIfChanged(ref knownModsByName, value);
        }

        public PoeQueryModsGroupType GroupType
        {
            get => groupType;
            set => this.RaiseAndSetIfChanged(ref groupType, value);
        }

        public float? MinGroupValue
        {
            get => minGroupValue;
            set => this.RaiseAndSetIfChanged(ref minGroupValue, value);
        }

        public float? MaxGroupValue
        {
            get => maxGroupValue;
            set => this.RaiseAndSetIfChanged(ref maxGroupValue, value);
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
            var result = new Dictionary<string, IPoeItemMod>();
            var badMods = new List<string>();
            foreach (var itemMod in staticData.ModsList)
            {
                if (result.ContainsKey(itemMod.Name))
                {
                    badMods.Add($"Duplicate mod detected: {itemMod.DumpToTextRaw()} and {result[itemMod.Name].DumpToTextRaw()} share the same key");
                    continue;
                }

                result[itemMod.Name] = itemMod;
            }

            if (badMods.Any())
            {
                Log.Instance.Warn($"[PoeModsEditorViewModel] Bad mods detected({badMods.Count})\n\n{badMods.DumpToTable()}");
            }
            return result;
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
                    knownMod = new PoeItemMod {CodeName = modModel.SelectedMod, Name = modModel.SelectedMod};
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