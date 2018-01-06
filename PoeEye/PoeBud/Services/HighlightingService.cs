using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Guards;
using JetBrains.Annotations;
using PoeBud.Models;
using PoeEye.StashGrid.Services;
using PoeShared;
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

        public IDisposable Highlight(IPoeTradeSolution solution)
        {
            var anchors = new CompositeDisposable();
            activeHighlighting.Disposable = anchors;

            if (solution == null)
            {
                return anchors;
            }
            
            Log.Instance.Debug($"Highlighting {solution.Items.Length} item(s)");

            foreach (var item in solution.Items)
            {
                var controller = poeStashHighlightService.AddHighlight(item.Position, item.Tab.StashType).AddTo(anchors);
                controller.IsFresh = true;
            }

            Observable.Timer(highlightPeriod).Subscribe(() => anchors.Dispose()).AddTo(anchors);
            return anchors;
        }
    }
}