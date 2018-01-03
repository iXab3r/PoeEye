using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
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
        private string html;
        private IPoeItem item;
        private string rawText;
        private bool useFastRendering;
        private string[] htmlImplicitMods;
        private string[] htmlExplicitMods;
        private string[] htmlUnknownMods;
        private readonly IConverter<IPoeItemMod, string> htmlConverter;

        public PoeItemModsViewModel()
        {
            htmlConverter = (IConverter<IPoeItemMod, string>)Application.Current.FindResource("DefaultPoeItemModToHtmlConverterKey");
            
            this.WhenAnyValue(x => x.Item)
                .Select(x => x?.Mods ?? new IPoeItemMod[0])
                .Subscribe(
                    mods =>
                    {
                        if (mods == null)
                        {
                            RawText = Html = null;
                            HtmlExplicitMods = HtmlImplicitMods = htmlUnknownMods = new string[0];
                        }
                        else
                        {
                            RebuildHtml(mods);
                            RebuildHtmlModList(mods);
                            RebuildText(mods);
                        }
                    })
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

        public string[] HtmlImplicitMods
        {
            get { return htmlImplicitMods; }
            set { this.RaiseAndSetIfChanged(ref htmlImplicitMods, value); }
        }

        public string[] HtmlExplicitMods
        {
            get { return htmlExplicitMods; }
            set { this.RaiseAndSetIfChanged(ref htmlExplicitMods, value); }
        }

        public string[] HtmlUnknownMods
        {
            get { return htmlUnknownMods; }
            set { this.RaiseAndSetIfChanged(ref htmlUnknownMods, value); }
        }

        private void RebuildHtmlModList(IPoeItemMod[] mods)
        {
            Guard.ArgumentNotNull(mods, nameof(mods));

            var implicitMods = mods.Where(x => x.ModType == PoeModType.Implicit);
            var explicitMods = mods.Where(x => x.ModType == PoeModType.Explicit);
            var unknownMods = mods.Where(x => x.ModType == PoeModType.Unknown);

            HtmlImplicitMods = implicitMods.Select(htmlConverter.Convert).ToArray();
            HtmlExplicitMods = explicitMods.Select(htmlConverter.Convert).ToArray();
            HtmlUnknownMods = unknownMods.Select(htmlConverter.Convert).ToArray();
        }

        private void RebuildHtml(IPoeItemMod[] mods)
        {
            Guard.ArgumentNotNull(mods, nameof(mods));

            var result = new StringBuilder();

            var implicitMods = mods.Where(x => x.ModType == PoeModType.Implicit).ToArray();
            var explicitMods = mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            var unknownMods = mods.Where(x => x.ModType == PoeModType.Unknown).ToArray();

            implicitMods.ForEach(mod => result.Append($"{htmlConverter.Convert(mod)}"));
            if (implicitMods.Any())
            {
                result.AppendLine($"-------------------------------------<br/>");
            }
            explicitMods.ForEach(mod => result.Append($"{htmlConverter.Convert(mod)}"));
            unknownMods.ForEach(mod => result.Append($"{htmlConverter.Convert(mod)}"));
            
            Html = result.ToString();
        }
        
        private void RebuildText(IPoeItemMod[] mods)
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
            
            RawText = result.ToString().Trim('\n','\r', ' ');
        }
    }
}