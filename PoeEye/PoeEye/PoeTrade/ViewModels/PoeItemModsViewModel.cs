using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using DynamicData.Binding;
using Guards;
using PoeEye.Converters;
using PoeShared.Common;
using PoeShared.Scaffolding;
using ReactiveUI;
using TypeConverter;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeItemModsViewModel : DisposableReactiveObject, IPoeItemModsViewModel
    {
        private readonly IConverter<IPoeItemMod, string> modToHtmlConverter = new PoeItemModToHtmlConverter();

        private string html;
        private IPoeItem item;
        private string rawText;
        private bool useFastRendering;

        public PoeItemModsViewModel()
        {
            this.WhenValueChanged(x => x.Item)
                .Select(item => item == null ? new IPoeItemMod[0] : item.Mods)
                .Subscribe(mods => Html = RebuildHtml(mods))
                .AddTo(Anchors);
            
            this.WhenValueChanged(x => x.Item)
                .Select(item => item == null ? new IPoeItemMod[0] : item.Mods)
                .Subscribe(mods => RawText = RebuildText(mods))
                .AddTo(Anchors);
        }

        public IPoeItem Item
        {
            get { return item; }
            set { this.RaiseAndSetIfChanged(ref item, value); }
        }
        
        public string Html
        {
            get { return html; }
            private set { this.RaiseAndSetIfChanged(ref html, value); }
        }

        public string RawText
        {
            get { return rawText; }
            private set { this.RaiseAndSetIfChanged(ref rawText, value); }
        }

        public bool UseFastRendering
        {
            get { return useFastRendering; }
            set { this.RaiseAndSetIfChanged(ref useFastRendering, value); }
        }
        
        private string RebuildHtml(IPoeItemMod[] mods)
        {
            Guard.ArgumentNotNull(mods, nameof(mods));

            var result = new StringBuilder();

            var implicitMods = mods.Where(x => x.ModType == PoeModType.Implicit);
            var explicitMods = mods.Where(x => x.ModType == PoeModType.Explicit);
            var unknownMods = mods.Where(x => x.ModType == PoeModType.Unknown);

            foreach (var mod in implicitMods)
            {
                var html = modToHtmlConverter.Convert(mod);
                result.Append($"{html}");
            }
            result.Append($"-------------------------------------<br/>");
            foreach (var mod in explicitMods)
            {
                var html = modToHtmlConverter.Convert(mod);
                result.Append($"{html}");
            }
            result.Append($"-------------------------------------<br/>");
            foreach (var mod in unknownMods)
            {
                var html = modToHtmlConverter.Convert(mod);
                result.Append($"{html}");
            }
            
            return result.ToString();
        }
        
        private string RebuildText(IPoeItemMod[] mods)
        {
            Guard.ArgumentNotNull(mods, nameof(mods));

            var result = new StringBuilder();

            var implicitMods = mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();
            var explicitMods = mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            var unknownMods = mods.Where(x => x.ModType == PoeModType.Unknown).ToArray();

            var longestName = mods.Any() ? mods.Select(x => x.Name.Length).Max() : 0;

            implicitMods.ForEach(x => result.AppendLine($"{x.Name.PadRight(longestName)} {x.TierInfo}"));
            if (implicitMods.Any())
            {
                result.AppendLine($"-------------------------------------");
            }
            explicitMods.ForEach(x => result.AppendLine($"{x.Name.PadRight(longestName)} {x.TierInfo}"));
            unknownMods.ForEach(x => result.AppendLine($"{x.Name.PadRight(longestName)} {x.TierInfo}"));
            
            return result.ToString().Trim('\n','\r', ' ');
        }
    }
}