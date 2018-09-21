using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;
using Common.Logging;
using Guards;
using PoeShared;
using PoeShared.Common;
using PoeShared.Scaffolding;
using TypeConverter;

namespace PoeEye.Converters
{
    internal sealed class PoeItemModToHtmlConverter : IValueConverter, IConverter<IPoeItemMod, string>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeItemModToHtmlConverter));

        private readonly ConcurrentDictionary<string, Regex> cache = new ConcurrentDictionary<string, Regex>();

        private readonly ICollection<ModParserConfig> parsingSettings = new List<ModParserConfig>();

        public PoeItemModToHtmlConverter()
        {
            Preconfigure().ForEach(parsingSettings.Add);
        }

        public Color DefaultTextColor { get; set; }
        public Color FireRelatedTextColor { get; set; }
        public Color ColdRelatedTextColor { get; set; }
        public Color LightningRelatedTextColor { get; set; }
        public Color ChaosRelatedTextColor { get; set; }
        public Color PhysicalRelatedTextColor { get; set; }
        public Color ElementalRelatedTextColor { get; set; }

        public Color LifeRelatedTextColor { get; set; }
        public Color ManaRelatedTextColor { get; set; }

        public Color StrengthRelatedTextColor { get; set; }
        public Color IntelligenceRelatedTextColor { get; set; }
        public Color DexterityRelatedTextColor { get; set; }

        public Color TotalGroupColor { get; set; }
        public Color PseudoGroupColor { get; set; }
        public Color EnchantGroupColor { get; set; }
        public Color CraftGroupColor { get; set; }

        public string Convert(IPoeItemMod mod)
        {
            IEnumerable<Tuple<string, string>> history;
            return Convert(mod, out history);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IPoeItemMod mod))
            {
                return Binding.DoNothing;
            }

            var modName = Convert(mod);
            return modName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private IEnumerable<ModParserConfig> Preconfigure()
        {
            yield return new ModParserConfig
            {
                Expression = @"(?'min'[\d\.\,]+)\s*(?:-|to)\s*(?'max'[\d\.\,]+)",
                Functor = (text, match) => ReplaceGroup(match, match.Groups[0], text, AddAverage(match))
            };
            yield return new ModParserConfig
            {
                Expression = CreateReplacementRegex(@"(total:\s*)"),
                Functor = (text, match) => ReplaceGroup(match, text, AddGroup("total", DefaultTextColor, TotalGroupColor))
            };
            yield return new ModParserConfig
            {
                Expression = CreateReplacementRegex(@"(pseudo:\s*)"),
                Functor = (text, match) => ReplaceGroup(match, text, AddGroup("pseudo", DefaultTextColor, PseudoGroupColor))
            };
            yield return new ModParserConfig
            {
                Expression = CreateReplacementRegex(@"(enchanted\s*)"),
                Functor = (text, match) => ReplaceGroup(match, text, AddGroup("enchanted", DefaultTextColor, EnchantGroupColor))
            };
            yield return new ModParserConfig
            {
                Expression = CreateReplacementRegex(@"(crafted\s*)"),
                Functor = (text, match) => ReplaceGroup(match, text, AddGroup("crafted", DefaultTextColor, CraftGroupColor))
            };
            yield return new ModParserConfig
            {
                Expression = "(to Cold Resistance)|(Adds.*Cold Damage)",
                Functor = (text, match) => WrapInSpan(text, ColdRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(to Fire Resistance)|(Adds.*Fire Damage)",
                Functor = (text, match) => WrapInSpan(text, FireRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(to Lightning Resistance)|(Adds.*Lightning Damage)",
                Functor = (text, match) => WrapInSpan(text, LightningRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(to Chaos Resistance)|(Adds.*Chaos Damage)",
                Functor = (text, match) => WrapInSpan(text, ChaosRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "((?:increased|decreased) Cast Speed)|((?:increased|decreased) Spell Damage)",
                Functor = (text, match) => WrapInSpan(text, IntelligenceRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "((?:increased|decreased) Attack Speed)",
                Functor = (text, match) => WrapInSpan(text, DexterityRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "((?:increased|decreased) Intelligence)|(to Intelligence)|(to maximum Energy Shield)|(increased Energy Shield)",
                Functor = (text, match) => WrapInSpan(text, IntelligenceRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "((?:increased|decreased) Dexterity)|(to Dexterity)|(to Evasion)|((?:increased|decreased) Evasion Rating)",
                Functor = (text, match) => WrapInSpan(text, DexterityRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "((?:increased|decreased) Strength)|(to Strength)|(to Armour)|((?:increased|decreased) Global Defences)",
                Functor = (text, match) => WrapInSpan(text, StrengthRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(Physical Damage)",
                Functor = (text, match) => WrapInSpan(text, PhysicalRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(Elemental Damage)|(to all Elemental Resistances)|(total Elemental Resistances)",
                Functor = (text, match) => WrapInSpan(text, ElementalRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(to maximum Life)|(Life gained.*hit)|(Life Regenerated)",
                Functor = (text, match) => WrapInSpan(text, LifeRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(to maximum Mana)|(Mana Regeneration)|(Mana Regenerated)",
                Functor = (text, match) => WrapInSpan(text, ManaRelatedTextColor)
            };
            yield return new ModParserConfig
            {
                Expression = "(.*)",
                Functor = (text, match) => WrapInSpan(text, DefaultTextColor)
            };
        }

        public string Convert(IPoeItemMod mod, out IEnumerable<Tuple<string, string>> history)
        {
            Guard.ArgumentNotNull(mod, nameof(mod));

            var name = mod.Name ?? "(Unknown mod - no name specified)";
            if (mod.Origin == PoeModOrigin.Craft)
            {
                name = $"crafted {name}";
            }

            if (mod.Origin == PoeModOrigin.Enchant)
            {
                name = $"enchanted {name}";
            }

            var resultHistory = new List<Tuple<string, string>>();
            resultHistory.Add(new Tuple<string, string>(string.Empty, name));
            foreach (var config in parsingSettings)
            {
                var regex = cache.GetOrAdd(config.Expression, expr => new Regex(expr, RegexOptions.Compiled | RegexOptions.IgnoreCase));

                var match = regex.Match(name);

                if (!match.Success)
                {
                    continue;
                }

                name = config.Functor(name, match);
                resultHistory.Add(new Tuple<string, string>(config.Expression, name));
            }

            history = resultHistory;
            return name;
        }

        public static string CreateReplacementRegex(string expression)
        {
            return $@"(?<=^|>){expression}";
        }

        public static string WrapTierInfo(string input, Color color)
        {
            return $"<span style='font-style: italic; margin:10px; color: {ToRgb(color)}'>{input}</span>";
        }

        public static string WrapInSpan(string input, Color color)
        {
            return $"<span style='color: {ToRgb(color)}'>{input}</span>";
        }

        public static string AddGroup(string input, Color color, Color bgColor)
        {
            return
                $"<span style='font-size: 95%; color: {ToRgb(color)}; background-color:{ToRgb(bgColor)};  padding-left:5px;padding-right:5px; margin-right:5px;'>{input}</span>";
        }

        public static string AddAverage(Match match)
        {
            var minRaw = match.Groups["min"].Value;
            var maxRaw = match.Groups["max"].Value;
            try
            {
                var min = double.Parse(minRaw);
                var max = double.Parse(maxRaw);
                var avg = min + (max - min) / 2;
                return $"{match.Value} (~{avg.ToString("0.0", CultureInfo.InvariantCulture)})";
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to parse input {match.Value}, min: '{match.Groups["min"]}', max: '{match.Groups["max"]}'");
                return string.Empty;
            }
            
        }

        private static string ReplaceGroup(
            Match match, Group group, string input, string replacement)
        {
            var firstPart = input.Substring(0, group.Index);
            var secondPart = input.Substring(group.Index + group.Length);
            var fullReplace = firstPart + replacement + secondPart;
            return fullReplace;
        }

        private static string ReplaceGroup(
            Match match, string input, string replacement)
        {
            var group = match.Groups[1];
            return ReplaceGroup(match, group, input, replacement);
        }

        private static string ToRgb(Color color)
        {
            return $"rgb({color.R}, {color.G}, {color.B})";
        }

        private static string StripHtml(string input)
        {
            var tagsExpression = new Regex(@"</?.+?>");
            return tagsExpression.Replace(input, string.Empty);
        }

        private struct ModParserConfig
        {
            public string Expression { get; set; }

            public Func<string, Match, string> Functor { get; set; }
        }
    }
}