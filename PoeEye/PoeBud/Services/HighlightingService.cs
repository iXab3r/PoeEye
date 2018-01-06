using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeBud.Models;
using PoeEye.StashGrid.Services;
using PoeShared.Scaffolding;

namespace PoeBud.Services {
    
    internal sealed class HighlightingService : DisposableReactiveObject, IHighlightingService
    {
        private readonly TimeSpan highlightPeriod = TimeSpan.FromSeconds(10);
        
        private readonly IPoeStashHighlightService poeStashHighlightService;
        
        private readonly SerialDisposable activeHighlighting = new SerialDisposable();
        
        public HighlightingService( [NotNull] IPoeStashHighlightService poeStashHighlightService)
        {
            Guard.ArgumentNotNull(poeStashHighlightService, nameof(poeStashHighlightService));

            this.poeStashHighlightService = poeStashHighlightService;

            activeHighlighting.AddTo(Anchors);
        }

        public void Highlight(IPoeTradeSolution solution)
        {
            Guard.ArgumentNotNull(solution, nameof(solution));

            var anchors = new CompositeDisposable();
            activeHighlighting.Disposable = anchors;

            foreach (var item in solution.Items)
            {
                var tab = solution.Tabs.First(x => x.Idx == item.TabIndex);
                var controller = poeStashHighlightService.AddHighlight(item.Position, tab.StashType).AddTo(anchors);
                controller.IsFresh = true;
                controller.ToolTipText = tab.Name;
            }

            Observable.Timer(highlightPeriod).Subscribe(() => anchors.Dispose()).AddTo(anchors);
        }
    }
}