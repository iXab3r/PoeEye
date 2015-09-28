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

        public MainWindowTabViewModel(
            [NotNull] TradesListViewModel tradesListViewModel,
            [NotNull] PoeQueryViewModel queryViewModel)
        {
            Guard.ArgumentNotNull(() => tradesListViewModel);
            Guard.ArgumentNotNull(() => queryViewModel);

            TradesListViewModel = tradesListViewModel;
            var command = ReactiveCommand.Create();
            command.Subscribe(SearchCommandExecute);

            searchCommand = command;

            tradesListViewModel
                .WhenAnyValue(x => x.LastUpdateTimestamp)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(LastUpdateTimestamp)));

            tradesListViewModel
                .WhenAnyValue(x => x.RecheckTimeout)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(RecheckTimeout)));

            QueryViewModel = queryViewModel;
        }

        private void SearchCommandExecute(object arg)
        {
            var queryBuilder = arg as Func<IPoeQuery>;
            if (queryBuilder == null)
            {
                return;
            }
            var query = queryBuilder();
            /*var query = new PoeQueryBuilder()
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
            };*/

            RecheckTimeout = TimeSpan.FromSeconds(30);
            TradesListViewModel.Query = query;
        }

        public TimeSpan RecheckTimeout
        {
            get { return TradesListViewModel.RecheckTimeout; }
            set { TradesListViewModel.RecheckTimeout = value; }
        }

        public TradesListViewModel TradesListViewModel { get; }

        public ICommand SearchCommand => searchCommand;

        public DateTime LastUpdateTimestamp => TradesListViewModel.LastUpdateTimestamp;

        public string TabName
        {
            get { return tabName; }
            set { this.RaiseAndSetIfChanged(ref tabName, value); }
        }

        public PoeQueryViewModel QueryViewModel { get; }
    }
}