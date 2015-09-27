namespace PoeEyeUi
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using ReactiveUI;

    internal sealed class MainWindowViewModel : ReactiveObject
    {
        private readonly IFactory<MainWindowTabViewModel> tabFactory;
        private readonly ReactiveList<MainWindowTabViewModel> tabsList = new ReactiveList<MainWindowTabViewModel>();

        private int tabIdx = 0;
        private MainWindowTabViewModel selectedItem;
        private readonly ReactiveCommand<object> createNewTabCommand;
        private readonly ReactiveCommand<object> closeTabCommand;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory)
        {
            Guard.ArgumentNotNull(() => tabFactory);

            this.tabFactory = tabFactory;
            createNewTabCommand = ReactiveCommand.Create();
            createNewTabCommand.Subscribe(CreateNewTabCommandExecuted);

            closeTabCommand = ReactiveCommand.Create();
            closeTabCommand.Subscribe(RemoveTabCommandExecuted);

            this.tabsList
                .ItemsAdded
                .Subscribe(x => SelectedItem = x);
        }

        public ICommand CreateNewTabCommand => createNewTabCommand;

        public ICommand CloseTabCommand => closeTabCommand;

        public ReactiveList<MainWindowTabViewModel> TabsList => tabsList;

        public MainWindowTabViewModel SelectedItem
        {
            get { return selectedItem; }
            set { this.RaiseAndSetIfChanged(ref selectedItem, value); }
        }

        private void CreateNewTabCommandExecuted(object o)
        {
            var newTab = tabFactory.Create();
            newTab.TabName = $"Tab #{tabIdx++}";
            tabsList.Add(newTab);
        }

        private void RemoveTabCommandExecuted(object o)
        {
            var tab = o as MainWindowTabViewModel;
            if (tab == null)
            {
                return;
            }
            tabsList.Remove(tab);
        }
    }
}