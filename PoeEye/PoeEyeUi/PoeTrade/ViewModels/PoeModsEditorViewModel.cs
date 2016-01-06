namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Utilities;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeModsEditorViewModel : DisposableReactiveObject
    {
        private readonly IFactory<PoeModViewModel, ISuggestionProvider> modsViewModelsFactor;
        private readonly ReactiveList<PoeModViewModel> modsCollection = new ReactiveList<PoeModViewModel>() { ChangeTrackingEnabled = true };
        private readonly ReactiveCommand<object> addModCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> removeModCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> clearModsCommand = ReactiveCommand.Create();

        private readonly ISuggestionProvider modsSuggestionProvider;

        public PoeModsEditorViewModel(
            [NotNull] IPoeQueryInfoProvider queryInfoProvider,
            [NotNull] IFactory<PoeModViewModel, ISuggestionProvider> modsViewModelsFactor,
            [NotNull] IFactory<ISuggestionProvider, string[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => modsViewModelsFactor);
            Guard.ArgumentNotNull(() => queryInfoProvider);
            Guard.ArgumentNotNull(() => suggestionProviderFactory);

            this.modsViewModelsFactor = modsViewModelsFactor;

            Mods = modsCollection;

            KnownMods = queryInfoProvider
                .ModsList
                .Select(x => x.Name)
                .ToArray();

            modsSuggestionProvider = suggestionProviderFactory.Create(KnownMods);

            addModCommand
                .Subscribe((_) => AddModCommandExecuted())
                .AddTo(Anchors);

            removeModCommand
                .Select(x => x as PoeModViewModel)
                .Where(x => x != null)
                .Subscribe(RemoveModCommandExecuted)
                .AddTo(Anchors);

            clearModsCommand
                .Subscribe(_ => ClearModsCommandExecuted())
                .AddTo(Anchors);

            AddModCommandExecuted();
        }

        public ReactiveList<PoeModViewModel> Mods { get; }

        public ICommand AddModCommand => addModCommand;

        public ICommand RemoveModCommand => removeModCommand;

        public ICommand ClearModsCommand => clearModsCommand;

        public string[] KnownMods { get; }

        private void AddModCommandExecuted()
        {
            AddMod();
        }

        private void RemoveModCommandExecuted(PoeModViewModel modToRemove)
        {
            Guard.ArgumentNotNull(() => modToRemove);

            using (Mods.SuppressChangeNotifications())
            {
                Mods.Remove(modToRemove);
            }
        }

        private void ClearModsCommandExecuted()
        {
            ClearMods();
        }

        public PoeModViewModel AddMod()
        {
            var newMod = modsViewModelsFactor.Create(modsSuggestionProvider);

            Mods.Add(newMod);

            return newMod;
        }

        public void ClearMods()
        {
            Mods.Clear();
        }

    }
}