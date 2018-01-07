using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;
using Guards;
using JetBrains.Annotations;
using PoeBud.Models;
using PoeEye.StashGrid.Services;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Services 
{
    internal sealed class HighlightingService : DisposableReactiveObject, IHighlightingService
    {
        private static readonly Color[] KnownBorderColors =
        {
            Color.FromRgb(29, 201, 49), 
            Color.FromRgb(255, 249, 140), 
            Color.FromRgb(98, 133, 255), 
            Color.FromRgb(255, 49, 57), 
            Color.FromRgb(255, 54, 220), 
        };
        
        private readonly IPoeStashHighlightService poeStashHighlightService;
        
        private readonly SerialDisposable activeHighlighting = new SerialDisposable();
        
        public HighlightingService(
            [NotNull] IPoeStashHighlightService poeStashHighlightService)
        {
            Guard.ArgumentNotNull(poeStashHighlightService, nameof(poeStashHighlightService));

            this.poeStashHighlightService = poeStashHighlightService;

            activeHighlighting.AddTo(Anchors);
        }

        public IDisposable Highlight(IPoeTradeSolution solution, TimeSpan duration)
        {
            var anchors = HighlightInternal(solution);
            
            Observable.Timer(duration).Subscribe(() => anchors.Dispose()).AddTo(anchors);
            return anchors;
        }
        
        public IDisposable Highlight(IPoeTradeSolution solution)
        {
            return HighlightInternal(solution);
        }
        
        private CompositeDisposable HighlightInternal(IPoeTradeSolution solution)
        {
            var anchors = new CompositeDisposable();
            activeHighlighting.Disposable = anchors;

            if (solution == null)
            {
                return anchors;
            }

            var colorByTab = solution.Items
                .Select(x => x.Tab)
                .Distinct(new LambdaComparer<IStashTab>((a, b) => a?.Id == b?.Id))
                .Select(delegate(IStashTab tab, int idx) { return new {Tab = tab, Colour = KnownBorderColors[idx % KnownBorderColors.Length]}; })
                .ToDictionary(x => x.Tab.Id, x => x.Colour);
            
            foreach (var item in solution.Items)
            {
                var controller = poeStashHighlightService.AddHighlight(item.Position, item.Tab.StashType).AddTo(anchors);
                controller.IsFresh = true;

                controller.BorderColor = colorByTab[item.Tab.Id];
            }

            return anchors;
        }
    }
}