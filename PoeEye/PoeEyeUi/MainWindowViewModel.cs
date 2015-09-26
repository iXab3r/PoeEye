namespace PoeEyeUi
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using ReactiveUI;

    internal sealed class MainWindowViewModel
    {
        private readonly IFactory<MainWindowTabViewModel> tabFactory;
        private readonly ObservableCollection<MainWindowTabViewModel> tabsList = new ObservableCollection<MainWindowTabViewModel>();

        private int tabIdx = 0;

        public MainWindowViewModel(
            [NotNull] IFactory<MainWindowTabViewModel> tabFactory)
        {
            Guard.ArgumentNotNull(() => tabFactory);
            
            this.tabFactory = tabFactory;
            var command = ReactiveCommand.Create();
            command.Subscribe(CreateNewTabCommandExecuted);

            CreateNewTabCommand = command;
        }

        public ICommand CreateNewTabCommand { get; }

        private void CreateNewTabCommandExecuted(object o)
        {
            var newTab = tabFactory.Create();
            newTab.TabName = $"Tab #{tabIdx++}";
            tabsList.Add(newTab);
        }

        public ObservableCollection<MainWindowTabViewModel> TabsList => tabsList;
    }
}