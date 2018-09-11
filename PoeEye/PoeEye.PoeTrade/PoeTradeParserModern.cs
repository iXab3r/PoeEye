using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using CsQuery;
using Guards;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.PoeTrade.Query;
using PoeShared.Scaffolding;
using PoeShared.StashApi.ProcurementLegacy;

namespace PoeEye.PoeTrade
{
    internal sealed class PoeTradeParserModern : IPoeTradeParser
    {
        private readonly IPoeTradeDateTimeExtractor dateTimeExtractor;
        private readonly IItemTypeAnalyzer itemTypeAnalyzer;

        public PoeTradeParserModern(
            [NotNull] IPoeTradeDateTimeExtractor dateTimeExtractor,
            [NotNull] IItemTypeAnalyzer itemTypeAnalyzer)
        {
            Guard.ArgumentNotNull(dateTimeExtractor, nameof(dateTimeExtractor));
            Guard.ArgumentNotNull(itemTypeAnalyzer, nameof(itemTypeAnalyzer));

            this.dateTimeExtractor = dateTimeExtractor;
            this.itemTypeAnalyzer = itemTypeAnalyzer;
        }

        public IPoeQueryResult ParseQueryResponse(string rawHtml)
        {
            Guard.ArgumentNotNull(rawHtml, nameof(rawHtml));

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeQueryResult
            {
                ItemsList = ExtractItems(parser),
                Id = ExtractQueryId(parser)
            };

            return result;
        }

        public IPoeStaticData ParseStaticData(string rawHtml)
        {
            Guard.ArgumentNotNull(rawHtml, nameof(rawHtml));

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeStaticData
            {
                ModsList = ExtractModsList(parser),
                LeaguesList = ExtractLeaguesList(parser),
                CurrenciesList = new IPoeCurrency[]
                {
                    new PoeCurrency {Name = "Blessed Orb", CodeName = KnownCurrencyNameList.BlessedOrb},
                    new PoeCurrency {Name = "Cartographer's Chisel", CodeName = KnownCurrencyNameList.CartographersChisel},
                    new PoeCurrency {Name = "Chaos Orb", CodeName = KnownCurrencyNameList.ChaosOrb},
                    new PoeCurrency {Name = "Chromatic Orb", CodeName = KnownCurrencyNameList.ChromaticOrb},
                    new PoeCurrency {Name = "Divine Orb", CodeName = KnownCurrencyNameList.DivineOrb},
                    new PoeCurrency {Name = "Exalted Orb", CodeName = KnownCurrencyNameList.ExaltedOrb},
                    new PoeCurrency {Name = "Gemcutter's Prism", CodeName = KnownCurrencyNameList.GemcuttersPrism},
                    new PoeCurrency {Name = "Jeweller's Orb", CodeName = KnownCurrencyNameList.JewellersOrb},
                    new PoeCurrency {Name = "Orb of Alchemy", CodeName = KnownCurrencyNameList.OrbOfAlchemy},
                    new PoeCurrency {Name = "Orb of Alteration", CodeName = KnownCurrencyNameList.OrbOfAlteration},
                    new PoeCurrency {Name = "Orb of Chance", CodeName = KnownCurrencyNameList.OrbOfChance},
                    new PoeCurrency {Name = "Orb of Fusing", CodeName = KnownCurrencyNameList.OrbOfFusing},
                    new PoeCurrency {Name = "Orb of Regret", CodeName = KnownCurrencyNameList.OrbOfRegret},
                    new PoeCurrency {Name = "Orb of Scouring", CodeName = KnownCurrencyNameList.OrbOfScouring},
                    new PoeCurrency {Name = "Regal Orb", CodeName = KnownCurrencyNameList.RegalOrb},
                    new PoeCurrency {Name = "Vaal Orb", CodeName = KnownCurrencyNameList.VaalOrb}
                },
                ItemTypes = new IPoeItemType[]
                {
                    new PoeItemType("Generic One-Handed Weapon", "1h"),
                    new PoeItemType("Generic Two-Handed Weapon", "2h"),
                    new PoeItemType("Bow", "Bow"),
                    new PoeItemType("Claw", "Claw"),
                    new PoeItemType("Dagger", "Dagger"),
                    new PoeItemType("One-Handed Axe", "One Hand Axe"),
                    new PoeItemType("One-Handed Mace", "One Hand Mace"),
                    new PoeItemType("One-Handed Sword", "One Hand Sword"),
                    new PoeItemType("Sceptre", "Sceptre"),
                    new PoeItemType("Staff", "Staff"),
                    new PoeItemType("Two-Handed Axe", "Two Hand Axe"),
                    new PoeItemType("Two-Handed Mace", "Two Hand Mace"),
                    new PoeItemType("Two-Handed Sword", "Two Hand Sword"),
                    new PoeItemType("Wand", "Wand"),
                    new PoeItemType("Body Armour", "Body Armour"),
                    new PoeItemType("Boots", "Boots"),
                    new PoeItemType("Gloves", "Gloves"),
                    new PoeItemType("Helmet", "Helmet"),
                    new PoeItemType("Shield", "Shield"),
                    new PoeItemType("Amulet", "Amulet"),
                    new PoeItemType("Belt", "Belt"),
                    new PoeItemType("Currency", "Currency"),
                    new PoeItemType("Divination Card", "Divination Card"),
                    new PoeItemType("Fishing Rods", "Fishing Rods"),
                    new PoeItemType("Flask", "Flask"),
                    new PoeItemType("Gem", "Gem"),
                    new PoeItemType("Jewel", "Jewel"),
                    new PoeItemType("Map", "Map"),
                    new PoeItemType("Quiver", "Quiver"),
                    new PoeItemType("Ring", "Ring"),
                    new PoeItemType("Vaal Fragments", "Vaal Fragments")
                }
            };
            return result;
        }

        private string ExtractQueryId(CQ parser)
        {
            var liveUri = parser["div[class='live-search-box alert-box'] a"]?.Attr("href");

            return liveUri;
        }

        private IPoeItemMod[] ExtractModsList(CQ parser)
        {
            var allModRows = parser["div[class='row explicit'] select option"].ToList();

            var allMods = allModRows
                          .Select(ParseItemModRow)
                          .Where(IsValid)
                          .Distinct(PoeItemMod.CodeNameComparer)
                          .Cast<IPoeItemMod>()
                          .ToArray();

            return allMods;
        }

        private static string[] ExtractLeaguesList(CQ parser)
        {
            var leaguesRows = parser["select[name=league] option"].ToList();
            var leaguesList = leaguesRows
                              .Select(ParseLeagueRow)
                              .Where(IsValid)
                              .Distinct()
                              .ToArray();
            return leaguesList;
        }

        private PoeItemMod ParseItemModRow(IDomObject row)
        {
            var result = new PoeItemMod
            {
                Name = row.InnerText,
                CodeName = row["value"]
            };

            var isImplicit = result.CodeName?.Contains("(implicit)");
            result.ModType = isImplicit != null && isImplicit.Value
                ? PoeModType.Implicit
                : PoeModType.Explicit;

            return result;
        }

        private static IPoeCurrency ParseCurrencyRow(IDomObject row)
        {
            var result = new PoeCurrency();
            CQ parser = row.Render();
            result.Name = parser.Text();
            result.CodeName = parser.Attr("value");
            return result;
        }

        private static string ParseLeagueRow(IDomObject row)
        {
            CQ parser = row.Render();
            var result = parser.Attr("value");
            return result;
        }

        private static bool IsValid(IPoeCurrency currency)
        {
            return !string.IsNullOrWhiteSpace(currency.CodeName);
        }

        private static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private IPoeItem ParseItemRow(IDomObject row)
        {
            CQ parser = row.Render();

            var result = new PoeItem
            {
                ItemIconUri = parser["div[class=icon] img"]?.Attr("src"),
                TradeForumUri = parser["td[class=item-cell] a[class^=title]"]?.Attr("href"),
                UserForumName = parser.Attr("data-seller"),
                UserIgn = parser.Attr("data-ign"),
                UserIsOnline = parser["tr[class=bottom-row] span[class~=success]"].Any(),
                Price = parser.Attr("data-buyout"),
                League = parser.Attr("data-league"),
                ThreadId = parser["span[class=click-button]"]?.Attr("data-thread"),
                Note = parser["span[class=item-note]"]?.Text(),
                Quality = parser["td[class=table-stats] td[data-name=q]"]?.Text(),
                Physical = parser["td[class=table-stats] td[data-name=quality_pd]"]?.Text(),
                Elemental = parser["td[class=table-stats] td[data-name=ed]"]?.Text(),
                AttacksPerSecond = parser["td[class=table-stats] td[data-name=aps]"]?.Text(),
                DamagePerSecond = parser["td[class=table-stats] td[data-name=quality_dps]"]?.Text(),
                PhysicalDamagePerSecond = parser["td[class=table-stats] td[data-name=quality_pdps]"]?.Text(),
                ElementalDamagePerSecond = parser["td[class=table-stats] td[data-name=edps]"]?.Text(),
                Armour = parser["td[class=table-stats] td[data-name=quality_armour]"]?.Text(),
                Evasion = parser["td[class=table-stats] td[data-name=quality_evasion]"]?.Text(),
                Shield = parser["td[class=table-stats] td[data-name=quality_shield]"]?.Text(),
                BlockChance = parser["td[class=table-stats] td[data-name=block]"]?.Text(),
                CriticalChance = parser["td[class=table-stats] td[data-name=crit]"]?.Text(),
                ItemLevel = parser["td[class=table-stats] td[data-name=level]"]?.Text(),
                Requirements = parser["td[class=item-cell] ul[class=requirements proplist]"]?.Text(),
                FirstSeen = dateTimeExtractor.ExtractTimestamp(parser["td[class=item-cell] span[class~=found-time-ago]"]?.Text()),
                Mods = ParseMods(parser),
                Modifications = ParseModificatins(parser),
                Links = ExtractLinksInfo(row),
                Rarity = ExtractItemRarity(row),
                PositionInsideTab = ExtractPositionInsideTab(parser),
                TabName = parser.Attr("data-tab"),
                Raw = row.Render()
            };

            ParseItemName(parser, ref result);

            var className = parser["tbody[class^=\"item item-live\"]"]?.Attr("class") ?? string.Empty;
            result.ItemState = className.IndexOf("item-gone", StringComparison.OrdinalIgnoreCase) >= 0
                ? PoeTradeState.Removed
                : PoeTradeState.New;

            result.Hash = parser["span[class=click-button]"]?.Attr("data-hash");
            if (string.IsNullOrWhiteSpace(result.Hash))
            {
                result.Hash = ParseItemId(className);
            }

            result.SuggestedPrivateMessage = PrepareTradeMessage(result);
            TrimProperties(result);
            return result;
        }

        private void ParseItemName(CQ parser, ref PoeItem item)
        {
            var nameText = parser.Attr("data-name");

            var info = itemTypeAnalyzer.ResolveTypeInfo(nameText);

            item.TypeInfo = info;
            item.ItemName = nameText;
        }

        private IPoeItemMod[] ParseMods(CQ parser)
        {
            var implicitMods = ExtractImplicitMods(parser);
            var explicitMods = ExtractExplicitMods(parser);
            return implicitMods.Concat(explicitMods).Where(IsValid).OfType<IPoeItemMod>().ToArray();
        }

        private PoeItemModificatins ParseModificatins(CQ parser)
        {
            var result = PoeItemModificatins.None;

            var explicitMods = ExtractExplicitMods(parser);

            result |= parser["td[class=item-cell] span[class~=corrupted]"].Any() ? PoeItemModificatins.Corrupted : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Name == "Shaped") ? PoeItemModificatins.Shaped : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Name == "Elder") ? PoeItemModificatins.Elder : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Name == "Mirrored") ? PoeItemModificatins.Mirrored : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Name == "Unidentified") ? PoeItemModificatins.Unidentified : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Origin == PoeModOrigin.Craft) ? PoeItemModificatins.Crafted : PoeItemModificatins.None;
            result |= explicitMods.Any(x => x.Origin == PoeModOrigin.Enchant) ? PoeItemModificatins.Enchanted : PoeItemModificatins.None;

            return result;
        }

        private static string PrepareTradeMessage(PoeItem item)
        {
            var location = "";
            if (!string.IsNullOrWhiteSpace(item.TabName) && item.PositionInsideTab != null)
            {
                location = $" (stash tab \"{item.TabName}\"; position: left {item.PositionInsideTab.Value.X + 1}, top {item.PositionInsideTab.Value.Y + 1})";
            }

            var message = string.IsNullOrWhiteSpace(item.Price)
                ? $"@{item.UserIgn} Hi, I would like to buy your {item.ItemName} listed in {item.League}{location}, offer is "
                : $"@{item.UserIgn} Hi, I would like to buy your {item.ItemName} listed for {item.Price} in {item.League}{location}";

            return message;
        }

        private static Point? ExtractPositionInsideTab(CQ parser)
        {
            var x = parser.Attr("data-x").ToIntOrDefault() ?? -1;
            var y = parser.Attr("data-y").ToIntOrDefault() ?? -1;
            if (x < 0 || y < 0)
            {
                return null;
            }

            return new Point(x, y);
        }

        private static float? ParseFloat(string rawValue)
        {
            float result;
            return !float.TryParse(rawValue, out result)
                ? (float?)null
                : result;
        }

        private static string ParseItemId(string rawValue)
        {
            var match = Regex.Match(rawValue, @"item-live-(?'id'[\d,a-z]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["id"].Value : string.Empty;
        }

        private static void TrimProperties<T>(T source)
        {
            var propertiesToProcess = typeof(T)
                                      .GetProperties()
                                      .Where(x => x.PropertyType == typeof(string))
                                      .Where(x => x.CanRead && x.CanWrite)
                                      .ToArray();

            foreach (var propertyInfo in propertiesToProcess)
            {
                var currentValue = (string)propertyInfo.GetValue(source);
                var newValue = currentValue ?? string.Empty;
                newValue = HttpUtility.HtmlDecode(newValue);
                newValue = newValue.Replace("\n", string.Empty);
                newValue = newValue.Replace("\r", string.Empty);
                newValue = newValue.Trim();

                propertyInfo.SetValue(source, newValue);
            }
        }

        private PoeItemRarity ExtractItemRarity(IDomObject row)
        {
            CQ parser = row.Render();

            var titleClass = parser["td[class=item-cell] a[class^=title]"]?.Attr("class");
            switch (titleClass)
            {
                case "title itemframe0":
                    return PoeItemRarity.Normal;
                case "title itemframe1":
                    return PoeItemRarity.Magic;
                case "title itemframe2":
                    return PoeItemRarity.Rare;
                case "title itemframe3":
                    return PoeItemRarity.Unique;
                case "title itemframe9":
                    return PoeItemRarity.Relic;
                default:
                    return PoeItemRarity.Unknown;
            }
        }

        private IPoeLinksInfo ExtractLinksInfoLegacy(IDomObject row)
        {
            CQ parser = row.Render();

            // sockets are in format [RGB]-[RGB]-... '-' indicates there is a link
            // i.e. RR-B = R + R-B = 1 + 2 link
            var rawLinksText = parser["span[class=sockets-raw]"]?.Text();
            return string.IsNullOrWhiteSpace(rawLinksText) ? default(IPoeLinksInfo) : new PoeLinksInfo(rawLinksText);
        }

        private IPoeLinksInfo ExtractLinksInfo(IDomObject row)
        {
            CQ parser = row.Render();

            var mappings = new Dictionary<string, string>
            {
                {"socketD", "G"},
                {"socketS", "R"},
                {"socketI", "B"},
                {"socketG", "W"},
                {"socketLink", "-"}
            };

            string GetMappedValue(string className)
            {
                var mapping = mappings.FirstOrDefault(x => className.IndexOf(x.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                return mapping.Value;
            }

            var socketClasses = parser["div[class=sockets-inner] div"]?.EmptyIfNull().Select(x => x.ClassName).ToArray();
            var rawLinksText = string.Join(string.Empty, socketClasses.Select(GetMappedValue));
            return string.IsNullOrWhiteSpace(rawLinksText) ? default(IPoeLinksInfo) : new PoeLinksInfo(rawLinksText);
        }


        private PoeItemMod[] ExtractExplicitMods(CQ parser)
        {
            var mods = parser["ul[class=mods] li"].Select(x => ExtractItemMod(x, PoeModType.Explicit)).ToArray();
            return mods;
        }

        private PoeItemMod[] ExtractImplicitMods(CQ parser)
        {
            return parser["ul[class=mods withline] li"].Select(x => ExtractItemMod(x, PoeModType.Implicit)).ToArray();
        }

        private PoeItemMod ExtractItemMod(CQ parser, PoeModType modType)
        {
            var result = new PoeItemMod
            {
                ModType = modType,
                CodeName = parser.Attr("data-name")?.Trim('#')
            };

            var nameElements = parser["li"]?.FirstOrDefault();
            if (nameElements != null)
            {
                var tierInfoChild = nameElements.ChildNodes.FirstOrDefault(x => x.ClassName?.StartsWith("item-affix") ?? false);
                if (tierInfoChild != null)
                {
                    nameElements.RemoveChild(tierInfoChild);
                    result.TierInfo = new CQ(tierInfoChild.Render()).Text();
                }

                result.Name = new CQ(nameElements.Render()).Text();
            }
            else
            {
                result.Name = "Unknown";
            }

            TrimProperties(result);

            if (result.Name.StartsWith("crafted", StringComparison.OrdinalIgnoreCase))
            {
                result.Origin = PoeModOrigin.Craft;
                result.Name = result.Name.Remove(0, "crafted".Length);
            }

            if (result.Name.StartsWith("enchanted", StringComparison.OrdinalIgnoreCase))
            {
                result.Origin = PoeModOrigin.Enchant;
                result.ModType = PoeModType.Implicit;
                result.Name = result.Name.Remove(0, "enchanted".Length);
            }

            if (result.Name.IndexOf("(total)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.ModType = PoeModType.Unknown;
            }

            if (result.Name.IndexOf("(pseudo)", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.ModType = PoeModType.Unknown;
            }

            return result;
        }

        public PoeItemMod ExtractItemMod(IDomObject itemModRow, PoeModType modType = PoeModType.Unknown)
        {
            CQ parser = itemModRow.Render();
            return ExtractItemMod(parser, modType);
        }

        private IPoeItem[] ExtractItems(CQ parser)
        {
            var rows = parser["tbody[id^=item-container-]"];

            var items = rows
                        .Select(ParseItemRow)
                        .Where(IsValid)
                        .ToArray();
            return items;
        }

        private IPoeCurrency[] ExtractCurrenciesList(CQ parser)
        {
            var currenciesRows = parser["select[name=buyout_currency] option"].ToList();

            var currencies = currenciesRows
                             .Select(ParseCurrencyRow)
                             .Where(IsValid)
                             .ToArray();
            return currencies;
        }

        private static bool IsValid(IPoeItem item)
        {
            return !string.IsNullOrWhiteSpace(item.ItemName);
        }

        private static bool IsValid(IPoeItemMod mod)
        {
            return !string.IsNullOrWhiteSpace(mod.CodeName) && !string.IsNullOrWhiteSpace(mod.Name) && mod.ModType != PoeModType.Unknown;
        }
    }
}