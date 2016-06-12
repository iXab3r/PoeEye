using System.Collections.Generic;
using System.Linq;
using Guards;
using JetBrains.Annotations;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeEye.ExileToolsApi.RealtimeApi.Entities;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.ExileToolsRealtimeApi.Converters
{
    internal class PoeQueryInfoToRealtimeSearchRequestConverter : IConverter<IPoeQueryInfo, RealtimeQuery>
    {
        private readonly IPoePriceCalculcator toChaosCalculcator;

        public PoeQueryInfoToRealtimeSearchRequestConverter([NotNull] IPoePriceCalculcator toChaosCalculcator)
        {
            Guard.ArgumentNotNull(() => toChaosCalculcator);

            this.toChaosCalculcator = toChaosCalculcator;
        }

        public RealtimeQuery Convert([NotNull] IPoeQueryInfo source)
        {
            Guard.ArgumentNotNull(() => source);

            var queries = new List<RealtimeQuery>
            {
                PrepareFilterByName(source),
                PrepareSocketsColorQuery(source),
                PreparePriceQuery(source),
                PrepareItemTypeQuery(source),
                PrepareModsQuery(source.ModGroups),
                CreateEqualToQuery("shop.sellerAccount", source.AccountName),
                CreateRangeQuery("propertiesPseudo.Weapon.estimatedQ20.Physical DPS", source.PdpsMin, source.PdpsMax),
                CreateRangeQuery("propertiesPseudo.Weapon.estimatedQ20.Total DPS", source.DpsMin, source.DpsMax),
                CreateRangeQuery("propertiesPseudo.Armour.estimatedQ20.Armour", source.ArmourMin, source.ArmourMax),
                CreateRangeQuery("propertiesPseudo.Armour.estimatedQ20.Energy Shield", source.ShieldMin, source.ShieldMax),
                CreateRangeQuery("propertiesPseudo.Armour.estimatedQ20.Evasion Rating", source.EvasionMin, source.EvasionMax),
                CreateRangeQuery("properties.Weapon.Attacks per Second", source.ApsMin, source.ApsMax),
                CreateRangeQuery("properties.Weapon.Critical Strike Chance", source.CritMin, source.CritMax),
                CreateRangeQuery("properties.Weapon.Total Damage.avg", source.DamageMin, source.DamageMax),
                CreateRangeQuery("properties.Weapon.Elemental DPS", source.EdpsMin, source.EdpsMax),
                CreateRangeQuery("properties.Armour.Chance to Block", source.BlockMin, source.BlockMax),
                CreateRangeQuery("properties.Quality", source.QualityMin, source.QualityMax),
                CreateRangeQuery("properties.Map.Item Quantity", source.IncQuantityMin, source.IncQuantityMin),
                CreateRangeQuery("properties.Map.Item Quantity", source.IncQuantityMin, source.IncQuantityMin),
                CreateEqualToQuery("attributes.league", source.League),
                CreateEqualToQuery("attributes.rarity", source.ItemRarity != null ? source.ItemRarity.ToString() : null),
                CreateEqualToQuery("shop.verified", source.OnlineOnly ? VerificationStatus.Yes : default(VerificationStatus?)),
        };
            var result = Merge(queries.ToArray());
            
            return result;
        }

        private RealtimeQuery PrepareModsQuery(IEnumerable<IPoeQueryModsGroup> modGroup)
        {
            var queries = modGroup
                .Where(x => x.GroupType == PoeQueryModsGroupType.And)
                .Select(x => x.Mods)
                .Select(PrepareModsQuery)
                .ToArray();

            return Merge(queries);
        }

        private RealtimeQuery PrepareModsQuery(IEnumerable<IPoeQueryRangeModArgument> mods)
        {
            return Merge(mods.Select(modArgument => CreateRangeQuery(modArgument.Mod.CodeName, modArgument.Min, modArgument.Max)).ToArray());
        }


        private RealtimeQuery PreparePriceQuery(IPoeQueryInfo source)
        {
            if (string.IsNullOrWhiteSpace(source.BuyoutCurrencyType))
            {
                return RealtimeQuery.Empty;
            }

            var minPrice = new PoePrice(source.BuyoutCurrencyType, source.BuyoutMin ?? 0);
            var maxPrice = new PoePrice(source.BuyoutCurrencyType, source.BuyoutMax ?? 0);

            var chaosEquivalentQuery = CreateRangeQuery(
                "shop.chaosEquiv",
                source.BuyoutMin == null ? default(float?) : toChaosCalculcator.GetEquivalentInChaosOrbs(minPrice).Value,
                source.BuyoutMax == null ? default(float?) : toChaosCalculcator.GetEquivalentInChaosOrbs(maxPrice).Value);

            return chaosEquivalentQuery;
        }

        private static RealtimeQuery PrepareFilterByName(IPoeQueryInfo source)
        {
            return CreateEqualToQuery("info.fullName", source.ItemName);
        }

        private static RealtimeQuery PrepareItemTypeQuery(IPoeQueryInfo source)
        {
            if (source.ItemType == null)
            {
                return RealtimeQuery.Empty;
            }
            return Merge(
                    CreateEqualToQuery("attributes.itemType", source.ItemType.ItemType),
                    CreateEqualToQuery("attributes.equipType", source.ItemType.EquipType));
        }

        private RealtimeQuery PrepareSocketsColorQuery(IPoeQueryInfo source)
        {
            return Merge(
                CreateRangeQuery("sockets.socketCount", source.SocketsMin, source.SocketsMax),
                CreateRangeQuery("sockets.largestLinkGroup", source.LinkMin, source.LinkMax),
                CreateRangeQuery("sockets.totalWhite", source.SocketsW),
                CreateRangeQuery("sockets.totalBlue", source.SocketsB),
                CreateRangeQuery("sockets.totalGreen", source.SocketsG),
                CreateRangeQuery("sockets.totalRed", source.SocketsR));
        }

        private static RealtimeQuery CreateEqualToQuery(string fieldName, object value)
        {
            if (value == null)
            {
                return RealtimeQuery.Empty;
            }
            if (value is string && string.IsNullOrWhiteSpace(value as string))
            {
                return RealtimeQuery.Empty;
            }

            return new RealtimeQuery
            {
                EqualTo = new Dictionary<string, object>()
                {
                    { fieldName, value },
                },
            };
        }

        private static RealtimeQuery CreateRangeQuery(string fieldName, float? minValue, float? maxValue = null)
        {
            if (minValue == null && maxValue == null)
            {
                return RealtimeQuery.Empty;
            }

            var result = new RealtimeQuery();
            if (minValue != null)
            {
                result.GreaterThan = new Dictionary<string, object>()
                {
                    {fieldName, minValue - 1},
                };
            }

            if (maxValue != null)
            {
                result.LessThan = new Dictionary<string, object>()
                {
                    {fieldName, maxValue + 1},
                };
            }

            return result;
        }

        private static RealtimeQuery Merge(params RealtimeQuery[] queries)
        {
            var result = new RealtimeQuery
            {
                EqualTo = queries
                    .Where(x => x.EqualTo != null)
                    .SelectMany(x => x.EqualTo)
                    .ToDictionary(pair => pair.Key, pair => pair.Value),
                GreaterThan = queries
                    .Where(x => x.GreaterThan != null)
                    .SelectMany(x => x.GreaterThan)
                    .ToDictionary(pair => pair.Key, pair => pair.Value),
                LessThan = queries
                    .Where(x => x.LessThan != null)
                    .SelectMany(x => x.LessThan)
                    .ToDictionary(pair => pair.Key, pair => pair.Value)
            };
            return result;
        }
    }
}