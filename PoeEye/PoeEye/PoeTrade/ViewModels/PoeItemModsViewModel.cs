using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
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
        private readonly IConverter<IPoeItemMod, string> htmlConverter;
        private string html;
        private ModHtml[] htmlExplicitMods;
        private ModHtml[] htmlImplicitMods;
        private ModHtml[] htmlUnknownMods;
        private IPoeItem item;
        private string rawText;
        private bool useFastRendering;

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
                            HtmlExplicitMods = HtmlImplicitMods = htmlUnknownMods = new ModHtml[0];
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

        public bool UseFastRendering
        {
            get => useFastRendering;
            set => this.RaiseAndSetIfChanged(ref useFastRendering, value);
        }

        public ModHtml[] HtmlImplicitMods
        {
            get => htmlImplicitMods;
            set => this.RaiseAndSetIfChanged(ref htmlImplicitMods, value);
        }

        public ModHtml[] HtmlExplicitMods
        {
            get => htmlExplicitMods;
            set => this.RaiseAndSetIfChanged(ref htmlExplicitMods, value);
        }

        public ModHtml[] HtmlUnknownMods
        {
            get => htmlUnknownMods;
            set => this.RaiseAndSetIfChanged(ref htmlUnknownMods, value);
        }

        public IPoeItem Item
        {
            get => item;
            set => this.RaiseAndSetIfChanged(ref item, value);
        }

        public string Html
        {
            get => html;
            private set => this.RaiseAndSetIfChanged(ref html, value);
        }

        public string RawText
        {
            get => rawText;
            private set => this.RaiseAndSetIfChanged(ref rawText, value);
        }

        private void RebuildHtmlModList(IPoeItemMod[] mods)
        {
            Guard.ArgumentNotNull(mods, nameof(mods));

            var implicitMods = mods.Where(x => x.ModType == PoeModType.Implicit);
            var explicitMods = mods.Where(x => x.ModType == PoeModType.Explicit);
            var unknownMods = mods.Where(x => x.ModType == PoeModType.Unknown);

            HtmlImplicitMods = implicitMods.Select(ToModHtml).ToArray();
            HtmlExplicitMods = explicitMods.Select(ToModHtml).ToArray();
            HtmlUnknownMods = unknownMods.Select(ToModHtml).ToArray();
        }

        private ModHtml ToModHtml(IPoeItemMod mod)
        {
            return new ModHtml
            {
                Name = htmlConverter.Convert(mod),
                TierInfo = string.IsNullOrEmpty(mod.TierInfo) ? null : PoeItemModToHtmlConverter.WrapTierInfo(mod.TierInfo, Colors.White)
            };
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
                result.AppendLine("-------------------------------------<br/>");
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
                result.AppendLine("-------------------------------------");
            }

            explicitMods.ForEach(x => result.AppendLine($"{x.Name.PadRight(longestName)} {x.TierInfo}"));
            unknownMods.ForEach(x => result.AppendLine($"{x.Name.PadRight(longestName)} {x.TierInfo}"));

            RawText = result.ToString().Trim('\n', '\r', ' ');
        }

        public struct ModHtml
        {
            public string Name { get; set; }

            public string TierInfo { get; set; }
        }
    }
}