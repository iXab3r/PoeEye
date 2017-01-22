using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeOracle.ViewModels;
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeOracle.Models
{
    internal sealed class ComplexSuggestionsDataSource : DisposableReactiveObject, ISuggestionsDataSource
    {
        private readonly IScheduler uiScheduler;
        private readonly IScheduler bgScheduler;
        private readonly ISuggestionProvider[] suggestionProviders;
        private readonly SerialDisposable activeQuery = new SerialDisposable();

        private bool isBusy;

        private string query;

        public ComplexSuggestionsDataSource(
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] ISuggestionProvider[] suggestionProviders)
        {
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => suggestionProviders);

            suggestionProviders.ForEach(Anchors.Add);
            activeQuery.AddTo(Anchors);

            this.uiScheduler = uiScheduler;
            this.bgScheduler = bgScheduler;
            this.suggestionProviders = suggestionProviders;

            this.WhenAnyValue(x => x.Query)
                .ObserveOn(uiScheduler)
                .Subscribe(Requery)
                .AddTo(Anchors);
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public string Query
        {
            get { return query; }
            set { this.RaiseAndSetIfChanged(ref query, value); }
        }

        public IReactiveList<IOracleSuggestionViewModel> Items { get; } = new ReactiveList<IOracleSuggestionViewModel>();

        private void Requery(string query)
        {
            var anchors = new CompositeDisposable();
            activeQuery.Disposable = anchors;

            var sources = suggestionProviders.Select(provider => RequestFromProvider(query, provider));

            IsBusy = true;
            Items.Clear();
            Observable.Merge(sources)
                .ObserveOn(uiScheduler)
                .Finally(() => IsBusy = false)
                .Subscribe(pack => pack.ForEach(Items.Add))
                .AddTo(anchors);
        }

        private IObservable<IOracleSuggestionViewModel[]> HandleException(Exception ex)
        {
            Log.HandleException(ex);
            return Observable.Empty<IOracleSuggestionViewModel[]>();
        }

        private IObservable<IOracleSuggestionViewModel[]> RequestFromProvider(
            string query,
            ISuggestionProvider provider)
        {
            return Observable
                .Start(() => provider.Request(query), bgScheduler)
                .Catch<IOracleSuggestionViewModel[], Exception>(HandleException)
                .Take(1);
        }
    }
}