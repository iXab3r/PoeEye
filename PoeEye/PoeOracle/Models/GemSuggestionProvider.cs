using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FuzzySearch;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeOracle.PoeDatabase;
using PoeOracle.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeOracle.Models
{
    internal sealed class GemSuggestionProvider : DisposableReactiveObject, ISuggestionProvider
    {
        private static readonly int MaxResults = 20;

        private readonly IFactory<OracleSuggestionGemViewModel, SkillGemModel> viewModelFactory;

        private readonly SerialDisposable requestAnchors = new SerialDisposable();
        private readonly IFuzzySearchService searchService;

        public GemSuggestionProvider(
            [NotNull] ISkillGemInfoProvider gemInfoProvider,
            [NotNull] IFactory<OracleSuggestionGemViewModel, SkillGemModel> viewModelFactory)
        {
            Guard.ArgumentNotNull(() => gemInfoProvider);
            Guard.ArgumentNotNull(() => viewModelFactory);

            this.viewModelFactory = viewModelFactory;

            searchService = new XSearchService<SkillGemModel>(gemInfoProvider.KnownGems, x => x.Name);
        }

        public IOracleSuggestionViewModel[] Request(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new IOracleSuggestionViewModel[0];
            }

            var anchors = new CompositeDisposable();
            requestAnchors.Disposable = anchors;

            var gemsToShow = searchService
                .Search(query)
                .Take(MaxResults)
                .Select(x => x.Match as SkillGemModel)
                .Where(x => x != null)
                .Select(viewModelFactory.Create)
                .ForEach(anchors.Add)
                .OfType<IOracleSuggestionViewModel>()
                .ToArray();

            return gemsToShow;
        }
    }
}