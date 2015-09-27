namespace PoeEyeUi
{
    using System;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.PoeTrade.Query;

    using PoeTrade.ViewModels;

    using ReactiveUI;

    internal sealed class MainWindowTabViewModel : ReactiveObject
    {
        private ICommand searchCommand;
        private string tabName;

        public MainWindowTabViewModel(TradesListViewModel tradesListViewModel)
        {
            Guard.ArgumentNotNull(() => tradesListViewModel);

            TradesListViewModel = tradesListViewModel;
            var command = ReactiveCommand.Create();
            command.Subscribe(SearchCommandExecute);

            searchCommand = command;

            tradesListViewModel
                .WhenAnyValue(x => x.LastUpdateTimestamp)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(this.LastUpdateTimestamp)));
        }

        private void SearchCommandExecute(object o)
        {
            var query = new PoeQuery()
            {
                Arguments = new IPoeQueryArgument[]
                {
                    new PoeQueryStringArgument("league", WellKnownLeagues.Warbands),
                    new PoeQueryStringArgument("name", "Temple map"),
                    new PoeQueryStringArgument("online", "x"),
                    new PoeQueryStringArgument("buyout", "x"),
                    new PoeQueryModArgument("Area is a large Maze"),
                    new PoeQueryModArgument("Area is #% larger") {Excluded = true},
                },
            };

            TradesListViewModel.RecheckTimeout = TimeSpan.FromSeconds(30);
            TradesListViewModel.Query = query;
        }

        public TradesListViewModel TradesListViewModel { get; }

        public ICommand SearchCommand => searchCommand;

        public DateTime LastUpdateTimestamp => TradesListViewModel.LastUpdateTimestamp;

        public string TabName
        {
            get { return tabName; }
            set { this.RaiseAndSetIfChanged(ref tabName, value); }
        }
    }
}