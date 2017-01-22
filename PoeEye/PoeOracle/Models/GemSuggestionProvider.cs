using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
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

            var gemsToShow = Search(query.Trim())
                .Take(MaxResults)
                .Where(x => x != null)
                .Select(viewModelFactory.Create)
                .ForEach(anchors.Add)
                .OfType<IOracleSuggestionViewModel>()
                .ToArray();

            return gemsToShow;
        }

        private IEnumerable<SkillGemModel> Search(string query)
        {
            var result = searchService
                .Search(query)
                .Select(x => x.Match as SkillGemModel)
                .ToArray();

            if (result.Length == 0)
            {
                // direct search failed, trying 'converted' search
                var converted = ConvertToEnglishLayout(query);
                if (!string.Equals(converted, query))
                {
                    return Search(converted);
                }
            }
            return result;
        }

        private static string ConvertToEnglishLayout(string source)
        {
        var result = source
                .Select(
                    x =>
                    {
                        char converted;
                        if (russianToEnglishMap.TryGetValue(x, out converted))
                        {
                            return converted;
                        }
                        return x;
                    }).ToArray();
            return new string(result);
        }

        private static readonly IDictionary<char, char> russianToEnglishMap = new Dictionary<char, char>()
        {
            { 'й', 'q' },
            { 'ц', 'w' },
            { 'у', 'e' },
            { 'к', 'r' },
            { 'е', 't' },
            { 'н', 'y' },
            { 'г', 'u' },
            { 'ш', 'i' },
            { 'щ', 'o' },
            { 'з', 'p' },
            { 'ф', 'a' },
            { 'ы', 's' },
            { 'в', 'd' },
            { 'а', 'f' },
            { 'п', 'g' },
            { 'р', 'h' },
            { 'о', 'j' },
            { 'л', 'k' },
            { 'д', 'l' },
            { 'я', 'z' },
            { 'ч', 'x' },
            { 'с', 'c' },
            { 'м', 'v' },
            { 'и', 'b' },
            { 'т', 'n' },
            { 'ь', 'm' },
        };
    }
}