namespace PoeEyeUi.PoeTrade.ViewModels
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
    using PoeShared.Utilities;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeModsEditorViewModel : DisposableReactiveObject, IPoeModsEditorViewModel
    {
        private readonly ReactiveCommand<object> addModCommand = ReactiveCommand.Create();

        private readonly ISuggestionProvider modsSuggestionProvider;
        private readonly IFactory<IPoeModViewModel, ISuggestionProvider> modsViewModelsFactory;
        private readonly ReactiveCommand<object> removeModCommand = ReactiveCommand.Create();

        private PoeQueryModsGroupType groupType;

        private float? maxGroupValue;

        private float? minGroupValue;

        public PoeModsEditorViewModel(
            [NotNull] IPoeQueryInfoProvider queryInfoProvider,
            [NotNull] IFactory<IPoeModViewModel, ISuggestionProvider> modsViewModelsFactory,
            [NotNull] IFactory<ISuggestionProvider, string[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => modsViewModelsFactory);
            Guard.ArgumentNotNull(() => queryInfoProvider);
            Guard.ArgumentNotNull(() => suggestionProviderFactory);

            this.modsViewModelsFactory = modsViewModelsFactory;

            KnownMods = queryInfoProvider
                .ModsList
                .Select(x => x.Name)
                .ToArray();

            modsSuggestionProvider = suggestionProviderFactory.Create(KnownMods);

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

        public string[] KnownMods { get; }

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
            Guard.ArgumentNotNull(() => modToRemove);

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

                var mod = new PoeQueryRangeModArgument(modModel.SelectedMod)
                {
                    Min = modModel.Min,
                    Max = modModel.Max
                };

                result.Add(mod);
            }
            return result.ToArray();
        }
    }
}