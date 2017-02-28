using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using CsQuery;
using Guards;
using Microsoft.Practices.ObjectBuilder2;
using Newtonsoft.Json.Linq;
using PoeOracle.Models;
using PoeShared.Scaffolding;

namespace PoeOracle.PoeDatabase
{
    internal sealed class SkillGemInfoProvider : ISkillGemInfoProvider
    {
        private readonly IDictionary<string, SkillGemExtendedInfo> gemsByName;

        public SkillGemInfoProvider()
        {
            gemsByName = LoadExtendedInfo()
                .ToDictionary(x => x.ItemName, x => x);

            KnownGems = LoadGemsInfoFromPoeFyi()
                .Select(BuildModel)
                .ToArray();
        }

        public SkillGemModel[] KnownGems { get; }

        private SkillGemModel BuildModel(SkillGemInfo skillGemInfo)
        {
            SkillGemExtendedInfo extendedInfo = null;
            gemsByName.TryGetValue(skillGemInfo.ItemName, out extendedInfo);

            var result = new SkillGemModel
            {
                Name = skillGemInfo.ItemName,
                CanBeBoughtBy = skillGemInfo.BoughtBy,
                RewardFor = PoeCharacterType.Unknown,
                SoldBy = skillGemInfo.SoldBy,
                Description = extendedInfo?.Description,
                QualityBonus = extendedInfo?.QualityBonus,
                IconUri = skillGemInfo.IconUri.ToUriOrDefault(),
                RequiredLevel = extendedInfo?.RequiredLevel ?? 0
            };
            return result;
        }

        private static IEnumerable<SkillGemExtendedInfo> LoadExtendedInfo()
        {
            var jsonGemsData =
                Assembly.GetExecutingAssembly().ReadResourceAsString($"{nameof(PoeDatabase)}.SkillGems.json");

            var json = JObject.Parse(jsonGemsData);
            foreach (var inner in json)
            {
                var raw = inner.Value.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }
                var gem = new SkillGemExtendedInfo(raw);
                if (string.IsNullOrWhiteSpace(gem.ItemName))
                {
                    continue;
                }
                yield return gem;
            }
        }

        private static IEnumerable<SkillGemInfo> LoadGemsInfoFromPoeFyi()
        {
            var rawGemsData =
                Assembly.GetExecutingAssembly().ReadResourceAsString($"{nameof(PoeDatabase)}.PoeFyiDump.html");
            var parser = new CQ(new StringReader(rawGemsData));

            foreach (var rawGem in parser["li[class=gem]"])
            {
                if (string.IsNullOrWhiteSpace(rawGem.OuterHTML))
                {
                    continue;
                }
                var gem = new SkillGemInfo(rawGem.OuterHTML);
                if (string.IsNullOrWhiteSpace(gem.ItemName))
                {
                    continue;
                }
                yield return gem;
            }
        }

        private class SkillGemInfo
        {
            private readonly CQ parser;
            private readonly string rawHtml;

            public SkillGemInfo(string rawHtml)
            {
                Guard.ArgumentNotNull(() => rawHtml);

                this.rawHtml = rawHtml;
                parser = new CQ(new StringReader(rawHtml));

                ItemName = parser["div"]?.Attr("data-name");

                IconUri = parser["div img"]?.Attr("src");
                if (!string.IsNullOrWhiteSpace(IconUri))
                {
                    IconUri = $"https://poe.fyi/{IconUri}";
                }

                SoldBy = parser["div ul[class=vendors] li"]
                    ?.Select(x => x.InnerText)
                    ?.Select(TrimAll)
                    ?.JoinStrings("\n");
                Tags = parser["li"].Attr("data-tags");
                BoughtBy = parser["div ul[class=classes] li"]
                               ?.Select(x => x.InnerText)
                               ?.Select(TrimAll)
                               ?.Select(TryParse)
                               .Where(x => x != null)
                               .Select(x => x.Value)
                               .Aggregate(PoeCharacterType.Unknown, (seed, x) => x |= seed) ?? PoeCharacterType.Unknown;
            }

            public string ItemName { get; }
            public string SoldBy { get; }
            public string Tags { get; }
            public string IconUri { get; }
            public PoeCharacterType BoughtBy { get; }

            private string TrimAll(string str)
            {
                return str?.Trim('\n', ' ', '\t');
            }

            private PoeCharacterType? TryParse(string str)
            {
                PoeCharacterType result;
                if (Enum.TryParse(str, out result))
                {
                    return result;
                }
                return null;
            }
        }

        private class SkillGemExtendedInfo
        {
            private readonly CQ parser;
            private readonly string rawHtml;

            public SkillGemExtendedInfo(string rawHtml)
            {
                Guard.ArgumentNotNull(() => rawHtml);

                this.rawHtml = rawHtml;
                parser = new CQ(new StringReader(rawHtml));

                ItemName = parser["span[class=ItemName]"].Text();
                Tags = parser["span[class=itemboxstatsgroup]"]?.FirstOrDefault()?.InnerText;
                Description = parser["span[class=itemboxstatsgroup] span[class=text-gem]"].Text();
                QualityBonus = parser["span[class=itemboxstatsgroup] span[class=item_magic]"].Text();
                RequiredLevel =
                    parser["span[class=itemboxstatsgroup]:contains('Requires Level') span"].Text().ToIntOrDefault();
            }

            public string ItemName { get; }
            public string Tags { get; }
            public string Description { get; }
            public string QualityBonus { get; }
            public int? RequiredLevel { get; }
        }
    }
}