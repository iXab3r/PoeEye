namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    internal sealed class PoeExplicitModsEditorViewModel
    {
        private readonly IFactory<PoeExplicitModViewModel, IPoeItemMod[]> modsViewModelsFactor;
        private readonly ReactiveList<PoeExplicitModViewModel> modsCollection = new ReactiveList<PoeExplicitModViewModel>() {ChangeTrackingEnabled = true};
        private readonly ReactiveCommand<object> addModCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> removeModCommand = ReactiveCommand.Create();

        private readonly IPoeItemMod[] knownPoeItemMods;

        public PoeExplicitModsEditorViewModel(
            [NotNull] IPoeQueryInfoProvider queryInfoProvider,
            [NotNull] IFactory<PoeExplicitModViewModel, IPoeItemMod[]> modsViewModelsFactor)
        {
            Guard.ArgumentNotNull(() => modsViewModelsFactor);
            Guard.ArgumentNotNull(() => queryInfoProvider);

            this.modsViewModelsFactor = modsViewModelsFactor;

            Mods = modsCollection;

            knownPoeItemMods = queryInfoProvider.ModsList.Where(x => x.ModType == PoeModType.Explicit).ToArray();

            addModCommand
                .Subscribe((_) => AddModCommandExecuted());

            removeModCommand
                .Select(x => x as PoeExplicitModViewModel)
                .Where(x => x != null)
                .Subscribe(RemoveModCommandExecuted);

            AddModCommandExecuted();
        }

        public ReactiveList<PoeExplicitModViewModel> Mods { get; }

        public ICommand AddModCommand => addModCommand;

        public ICommand RemoveModCommand => removeModCommand;

        private void AddModCommandExecuted()
        {
            var newMod = modsViewModelsFactor.Create(knownPoeItemMods);

            Mods.Add(newMod);
        }

        private void RemoveModCommandExecuted(PoeExplicitModViewModel modToRemove)
        {
            Guard.ArgumentNotNull(() => modToRemove);

            Mods.Remove(modToRemove);
        }

        
    }
}