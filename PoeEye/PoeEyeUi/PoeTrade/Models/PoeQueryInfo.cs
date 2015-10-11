﻿namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    using ViewModels;

    internal sealed class PoeQueryInfo : IPoeQueryInfo
    {
        public bool AlternativeArt { get; set; }

        public float? ApsMax { get; set; }

        public float? ApsMin { get; set; }

        public float? ArmourMax { get; set; }

        public float? ArmourMin { get; set; }

        public float? BlockMax { get; set; }

        public float? BlockMin { get; set; }

        public string BuyoutCurrencyType { get; set; }

        public float? BuyoutMax { get; set; }

        public float? BuyoutMin { get; set; }

        public bool BuyoutOnly { get; set; }

        public float? CritMax { get; set; }

        public float? CritMin { get; set; }

        public float? DamageMax { get; set; }

        public float? DamageMin { get; set; }

        public float? DpsMax { get; set; }

        public float? DpsMin { get; set; }

        public float? EdpsMax { get; set; }

        public float? EdpsMin { get; set; }

        public float? EvasionMax { get; set; }

        public float? EvasionMin { get; set; }

        public int? IncQuantityMax { get; set; }

        public int? IncQuantityMin { get; set; }

        public bool IsExpanded { get; set; }

        public string ItemBase { get; set; }

        public string ItemName { get; set; }

        public PoeItemRarity? ItemRarity { get; set; }

        public IPoeItemType ItemType { get; set; }

        public string League { get; set; }

        public string[] LeaguesList { get; set; }

        public int? LevelMax { get; set; }

        public int? LevelMin { get; set; }

        public int? LinkedB { get; set; }

        public int? LinkedG { get; set; }

        public int? LinkedR { get; set; }

        public int? LinkedW { get; set; }

        public int? LinkMax { get; set; }

        public int? LinkMin { get; set; }

        public bool NormalizeQuality { get; set; }

        public bool OnlineOnly { get; set; }

        public float? PdpsMax { get; set; }

        public float? PdpsMin { get; set; }

        public int? QualityMax { get; set; }

        public int? QualityMin { get; set; }

        public int? RDexMax { get; set; }

        public int? RDexMin { get; set; }

        public int? RIntMax { get; set; }

        public int? RIntMin { get; set; }

        public int? RLevelMax { get; set; }

        public int? RLevelMin { get; set; }

        public int? RStrMax { get; set; }

        public int? RStrMin { get; set; }

        public float? ShieldMax { get; set; }

        public float? ShieldMin { get; set; }

        public int? SocketsB { get; set; }

        public int? SocketsG { get; set; }

        public int? SocketsMax { get; set; }

        public int? SocketsMin { get; set; }

        public int? SocketsR { get; set; }

        public int? SocketsW { get; set; }

        public IPoeQueryRangeModArgument ImplicitMod { get; set; }

        public IPoeQueryRangeModArgument[] ExplicitMods { get; set; }


        private string[] FormatQueryDescriptionArray()
        {
            var blackList = new[]
            {
                nameof(League),
            };
            var nullableProperties = typeof(PoeQueryInfo)
                .GetProperties()
                .Where(x => !blackList.Contains(x.Name))
                .Where(x => x.PropertyType == typeof(int?)
                            || x.PropertyType == typeof(float?)
                            || x.PropertyType == typeof(string)
                            || x.PropertyType == typeof(IPoeItemType)
                            || x.PropertyType == typeof(PoeItemRarity?))
                .Where(x => x.CanRead)
                .ToArray();

            var result = new List<string>();
            foreach (var nullableProperty in nullableProperties)
            {
                var value = nullableProperty.GetValue(this);
                if (value == null)
                {
                    continue;
                }
                if (value is string && string.IsNullOrWhiteSpace(value as string))
                {
                    continue;
                }

                var formattedValue = $"{nullableProperty.Name}: {value}";
                result.Add(formattedValue);
            }
            return result.ToArray();
        }

        public override string ToString()
        {
            var descriptions = FormatQueryDescriptionArray();
            if (!descriptions.Any())
            {
                return null;
            }
            return String.Join("\r\n", descriptions);
        }
    }
}