using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using PoeShared.Scaffolding;
using ReactiveUI;
using WpfAutoCompleteControls.Editors;

namespace PoeEye.PoeTrade.Models
{
    internal sealed class ReactiveSuggestionProvider : DisposableReactiveObject, IReactiveSuggestionProvider
    {
        private IEnumerable<string> items;

        private ISuggestionProvider suggestionProvider;

        public ReactiveSuggestionProvider()
        {
            this.WhenAnyValue(x => x.Items)
                .Select(x => x.EmptyIfNull())
                .Subscribe(x => suggestionProvider = new FuzzySuggestionProvider(x))
                .AddTo(Anchors);
        }

        public IEnumerable GetSuggestions(string filter)
        {
            return suggestionProvider.GetSuggestions(filter);
        }

        public IEnumerable<string> Items
        {
            get => items;
            set => this.RaiseAndSetIfChanged(ref items, value);
        }
    }
}