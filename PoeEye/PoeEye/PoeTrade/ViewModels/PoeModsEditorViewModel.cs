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
        private readonly ReactiveCommand<object> addModCommand = ReactiveUI.Legacy.ReactiveCommand.Create();

        private readonly ISuggestionProvider modsSuggestionProvider;
        private readonly IFactory<IPoeModViewModel, ISuggestionProvider> modsViewModelsFactory;
        private readonly ReactiveCommand<object> removeModCommand = ReactiveUI.Legacy.ReactiveCommand.Create();

        private PoeQueryModsGroupType groupType;

        private float? maxGroupValue;

        private float? minGroupValue;

        private readonly IDictionary<string, IPoeItemMod> modsByName;

        public PoeModsEditorViewModel(
            [NotNull] IPoeStaticData staticData,
            [NotNull] IFactory<IPoeModViewModel, ISuggestionProvider> modsViewModelsFactory,
            [NotNull] IFactory<ISuggestionProvider, IEnumerable<string>> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(modsViewModelsFactory, nameof(modsViewModelsFactory));
            Guard.ArgumentNotNull(staticData, nameof(staticData));
            Guard.ArgumentNotNull(suggestionProviderFactory, nameof(suggestionProviderFactory));

            this.modsViewModelsFactory = modsViewModelsFactory;

            modsByName = staticData
                .ModsList
                .ToDictionary(x => x.Name, x => x);

            modsSuggestionProvider = suggestionProviderFactory.Create(modsByName.Keys.ToArray());

            addModCommand
                .Subscribe(_ => AddMod())
                .AddTo(Anchors);

            removeModCommand
                .Select(x => x as IPoeModViewModel)
                .Where(x => x != null)
                .Subscribe(RemoveModCommandExecuted)
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

        public IReactiveList<IPoeModViewModel> Mods { get; } = new ReactiveList<IPoeModViewModel> {ChangeTrackingEnabled = true};

        public IPoeModViewModel AddMod()
        {
            var newMod = modsViewModelsFactory.Create(modsSuggestionProvider);

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


        private void RemoveModCommandExecuted(IPoeModViewModel modToRemove)
        {
            Guard.ArgumentNotNull(modToRemove, nameof(modToRemove));

            using (Mods.SuppressChangeNotifications())
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
                if (!modsByName.TryGetValue(modModel.SelectedMod, out knownMod))
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
