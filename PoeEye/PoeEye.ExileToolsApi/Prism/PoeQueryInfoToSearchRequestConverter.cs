using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Guards;
using Nest;
using PoeEye.ExileToolsApi.Converters;
using PoeEye.ExileToolsApi.Entities;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.ExileToolsApi.Prism
{
    internal sealed class PoeQueryInfoToSearchRequestConverter : IConverter<IPoeQueryInfo, ISearchRequest>
    {
        private static readonly Regex RegexPrefix = new Regex(@"(regexp|rp|re|reg|r):(?<exp>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IPoePriceCalculcator toChaosCalculcator;
        public PoeQueryInfoToSearchRequestConverter(IPoePriceCalculcator toChaosCalculcator)
        {
            Guard.ArgumentNotNull(() => toChaosCalculcator);

            this.toChaosCalculcator = toChaosCalculcator;
        }


        public ISearchRequest Convert(IPoeQueryInfo source)
        {
            var result = new SearchRequest
            {
                Query = new FilteredQuery() { },
                Sort = new List<ISort>()
                {
                    new SortField { Field = "shop.chaosEquiv", Order = SortOrder.Ascending },
                    new SortField { Field = "shop.modified", Order = SortOrder.Ascending },
                },
                From = 0,
                Size = 50,
            };


            result.Query &= PrepareGeneralQuery(source);
            result.Query &= PrepareSocketsColorQuery(source);
            result.Query &= PrepareSocketsLinksQuery(source);
            result.Query &= PreparePriceQuery(source);
            result.Query &= PrepareFilterByAccountName(source);
            result.Query &= PrepareFilterByName(source);

            result.Query &= new BoolQuery
            {
                Should = CombineQueries(
                    CreateRangeBasedRequest("properties.Map.Map Tier", source.LevelMin, source.LevelMax),
                    CreateRangeBasedRequest("properties.Gem.Level", source.LevelMin, source.LevelMax)),
            };

            var modsQueries = new List<BoolQuery>();
            foreach (var modsGroup in source.ModGroups)
            {
                var boolQuery = new BoolQuery()
                {
                };
                switch (modsGroup.GroupType)
                {
                    case PoeQueryModsGroupType.And:
                        boolQuery.Must = PrepareModsQuery(modsGroup.Mods);
                        break;
                    case PoeQueryModsGroupType.Not:
                        boolQuery.MustNot = PrepareModsQuery(modsGroup.Mods);
                        break;
                    case PoeQueryModsGroupType.Count:
                        boolQuery.Should = PrepareModsQuery(modsGroup.Mods);
                        if (modsGroup.Min != null)
                        {
                            boolQuery.MinimumShouldMatch = new MinimumShouldMatch((int)modsGroup.Min.Value);
                        }
                        break;
                    case PoeQueryModsGroupType.Sum:
                        break;
                    case PoeQueryModsGroupType.If:
                        break;
                }

                modsQueries.Add(boolQuery);
            }

            foreach (var modsQuery in modsQueries)
            {
                result.Query &= modsQuery;
            }


            Log.Instance.Info($"[ExileToolsApi.Query] Elastic search query:\n{DumpQuery(result)}");

            return result;
        }

        private QueryBase PreparePriceQuery(IPoeQueryInfo source)
        {
            if (string.IsNullOrWhiteSpace(source.BuyoutCurrencyType))
            {
                return new BoolQuery();
            }

            var mustQueries = new[]
           {
                CreateExistsQuery("shop.hasPrice"),
                CreateWildcardQuery("shop.currency", $"*{source.BuyoutCurrencyType}*"),
                CreateRangeBasedRequest("shop.amount", source.BuyoutMin, source.BuyoutMax)
            };

            var minPrice = new PoePrice(source.BuyoutCurrencyType, source.BuyoutMin ?? 0);
            var maxPrice = new PoePrice(source.BuyoutCurrencyType, source.BuyoutMax ?? 0);

            var chaosEquivalentQuery = CreateRangeBasedRequest(
                    "shop.chaosEquiv",
                    toChaosCalculcator.GetEquivalentInChaosOrbs(minPrice).Value,
                    toChaosCalculcator.GetEquivalentInChaosOrbs(maxPrice).Value);

            return new BoolQuery { Must = CombineQueries(mustQueries) } || new BoolQuery { Must = CombineQueries(chaosEquivalentQuery) };
        }


        private QueryBase PrepareGeneralQuery(IPoeQueryInfo source)
        {
            var mustQueries = new[]
             {
                CreateRangeBasedRequest("propertiesPseudo.Weapon.estimatedQ20.Physical DPS", source.PdpsMin, source.PdpsMax),
                CreateRangeBasedRequest("propertiesPseudo.Weapon.estimatedQ20.Total DPS", source.DpsMin, source.DpsMax),
                CreateRangeBasedRequest("propertiesPseudo.Armour.estimatedQ20.Armour", source.ArmourMin, source.ArmourMax),
                CreateRangeBasedRequest("propertiesPseudo.Armour.estimatedQ20.Energy Shield", source.ShieldMin, source.ShieldMax),
                CreateRangeBasedRequest("propertiesPseudo.Armour.estimatedQ20.Evasion Rating", source.EvasionMin, source.EvasionMax),
                CreateRangeBasedRequest("properties.Weapon.Attacks per Second", source.ApsMin, source.ApsMax),
                CreateRangeBasedRequest("properties.Weapon.Critical Strike Chance", source.CritMin, source.CritMax),
                CreateRangeBasedRequest("properties.Weapon.Total Damage.avg", source.DamageMin, source.DamageMax),
                CreateRangeBasedRequest("properties.Weapon.Elemental DPS", source.EdpsMin, source.EdpsMax),
                CreateRangeBasedRequest("properties.Armour.Chance to Block", source.BlockMin, source.BlockMax),
                CreateRangeBasedRequest("properties.Quality", source.QualityMin, source.QualityMax),
                CreateRangeBasedRequest("properties.Map.Item Quantity", source.IncQuantityMin, source.IncQuantityMin),
                CreateRangeBasedRequest("properties.Map.Item Quantity", source.IncQuantityMin, source.IncQuantityMin),
                CreateTermQuery("attributes.league", source.League),
                CreateTermQuery("attributes.rarity", source.ItemRarity != null ? source.ItemRarity.ToString() : null),
                CreateTermQuery("shop.hasPrice", source.BuyoutOnly ? true : default(bool?)),
                CreateTermQuery("shop.verified", source.OnlineOnly ? VerificationStatus.Yes : default(VerificationStatus?)),
                CreateQuery(source.ItemType),
            };

            return new BoolQuery
            {
                Must = CombineQueries(mustQueries),
            };
        }

        private QueryBase PrepareFilterByName(IPoeQueryInfo source)
        {
            if (string.IsNullOrWhiteSpace(source.ItemName))
            {
                return new BoolQuery();
            }

            var shouldQueries = new List<IEnumerable<QueryBase>>();

            var regexModeMatch = RegexPrefix.Match(source.ItemName);
            if (regexModeMatch.Success)
            {
                var regex = regexModeMatch.Groups["exp"].Value.Trim();
                shouldQueries.AddRange(new[]
                {
                    CreateRegexQuery("info.fullName", regex, x => $".*{x}.*"),
                    CreateRegexQuery("info.tokenized.descrText", regex, x => $".*{x}.*"),
                    CreateRegexQuery("info.tokenized.flavourText", regex, x => $".*{x}.*"),
                    CreateRegexQuery("info.tokenized.prophecyText", regex, x => $".*{x}.*")
                });
            }
            else
            {
                shouldQueries.AddRange(new[]
                {
                    CreateWildcardQuery("info.fullName", source.ItemName, x => $"*{x}*"),
                    CreateWildcardQuery("info.tokenized.descrText", source.ItemName, x => $"*{x}*"),
                    CreateWildcardQuery("info.tokenized.flavourText", source.ItemName, x => $"*{x}*"),
                    CreateWildcardQuery("info.tokenized.prophecyText", source.ItemName, x => $"*{x}*")
                });
            }

            return new BoolQuery
            {
                Should = CombineQueries(shouldQueries.ToArray()),
                MinimumShouldMatch = shouldQueries.Any() ? 1 : 0,
            };
        }

        private QueryBase PrepareFilterByAccountName(IPoeQueryInfo source)
        {
            var shouldQueries = new[]
                {
                   CreateTermQuery("shop.sellerAccount", source.AccountName),
                   CreateTermQuery("shop.lastCharacterName", source.AccountName),
                };

            return new BoolQuery
            {
                Should = CombineQueries(shouldQueries),
                MinimumShouldMatch = shouldQueries.Any() ? 1 : 0,
            };
        }

        private QueryBase PrepareSocketsColorQuery(IPoeQueryInfo source)
        {
            var mustQueries = new[]
           {
                CreateRangeBasedRequest("sockets.socketCount", source.SocketsMin, source.SocketsMax),
                CreateRangeBasedRequest("sockets.largestLinkGroup", source.LinkMin, source.LinkMax),
                CreateRangeBasedRequest("sockets.totalWhite", source.SocketsW),
                CreateRangeBasedRequest("sockets.totalBlue", source.SocketsB),
                CreateRangeBasedRequest("sockets.totalGreen", source.SocketsG),
                CreateRangeBasedRequest("sockets.totalRed", source.SocketsR),
            };

            return new BoolQuery()
            {
                Must = CombineQueries(mustQueries),
            };
        }

        private QueryBase PrepareSocketsLinksQuery(IPoeQueryInfo source)
        {
            var sockets = string.Join(
               string.Empty,
               string.Join(string.Empty, Enumerable.Repeat("B", source.LinkedB ?? 0)),
               string.Join(string.Empty, Enumerable.Repeat("G", source.LinkedG ?? 0)),
               string.Join(string.Empty, Enumerable.Repeat("R", source.LinkedR ?? 0)),
               string.Join(string.Empty, Enumerable.Repeat("W", source.LinkedW ?? 0)));

            if (string.IsNullOrWhiteSpace(sockets))
            {
                return new BoolQuery();
            }

            var linkedSocketsQueryString = $"*{sockets}*";
            var shouldQueries = new[]
            {
                CreateWildcardQuery("sockets.sortedLinkGroup.0", linkedSocketsQueryString),
                CreateWildcardQuery("sockets.sortedLinkGroup.1", linkedSocketsQueryString),
                CreateWildcardQuery("sockets.sortedLinkGroup.2", linkedSocketsQueryString),
                CreateWildcardQuery("sockets.sortedLinkGroup.3", linkedSocketsQueryString),
                CreateWildcardQuery("sockets.sortedLinkGroup.4", linkedSocketsQueryString),
                CreateWildcardQuery("sockets.sortedLinkGroup.5", linkedSocketsQueryString),
            };

            return new BoolQuery()
            {
                Should = CombineQueries(shouldQueries),
                MinimumShouldMatch = 1
            };
        }

        private QueryContainer[] PrepareModsQuery(IEnumerable<IPoeQueryRangeModArgument> mods)
        {
            var groupQueryList = new List<IEnumerable<QueryBase>>();
            foreach (var modArgument in mods)
            {
                if (modArgument.Min == null && modArgument.Max == null)
                {
                    groupQueryList.Add(CreateExistsQuery(modArgument.Mod.CodeName + "*"));
                }
                else
                {
                    groupQueryList.Add(CreateTermRangeQuery(modArgument.Mod.CodeName, modArgument.Min, modArgument.Max));
                }
            }
            var groupQuery = groupQueryList.SelectMany(x => x).Select(x => new QueryContainer(x)).ToArray();
            return groupQuery;
        }

        private QueryContainer[] CombineQueries(params IEnumerable<QueryBase>[] queries)
        {
            return queries.SelectMany(x => x).Select(x => new QueryContainer(x)).ToArray();
        }

        private string DumpQuery(SearchRequest request)
        {
            var serializer = new JsonNetSerializer(new ConnectionSettings());

            var ms = new MemoryStream();

            serializer.Serialize(request, ms);
            return Encoding.ASCII.GetString(ms.ToArray());
        }

        private IEnumerable<QueryBase> CreateRangeBasedRequest(string fieldName, float? min, float? max = null)
        {
            if (min == null && max == null)
            {
                yield break;
            }

            foreach (var queryBase in CreateTermRangeQuery(fieldName, min, max))
            {
                yield return queryBase;
            }
        }

        private IEnumerable<QueryBase> CreateQuery(IPoeItemType itemType)
        {
            if (itemType == null || (string.IsNullOrWhiteSpace(itemType.ItemType) && string.IsNullOrWhiteSpace(itemType.EquipType)))
            {
                yield break;
            }

            var result = new BoolQuery()
            {
                Must = CombineQueries(
                        CreateTermQuery("attributes.itemType", itemType.ItemType),
                        CreateTermQuery("attributes.equipType", itemType.EquipType))
            };

            yield return result;
        }

        private IEnumerable<QueryBase> CreateTermRangeQuery(string fieldName, float? min, float? max)
        {
            var result = new NumericRangeQuery()
            {
                Field = fieldName,
            };
            if (min != null)
            {
                result.GreaterThanOrEqualTo = min.Value;
            }

            if (max != null)
            {
                result.LessThanOrEqualTo = max.Value;
            }

            yield return result;
        }

        private IEnumerable<QueryBase> CreateExistsQuery(string fieldName)
        {
            var result = new ExistsQuery()
            {
                Field = fieldName,
            };

            yield return result;
        }

        private IEnumerable<QueryBase> CreateTermQuery(string fieldName, object value)
        {
            if (value == null)
            {
                yield break;
            }
            if (value is string && string.IsNullOrWhiteSpace((string)value))
            {
                yield break;
            }

            var result = new TermQuery()
            {
                Field = fieldName,
                Value = value,
            };

            yield return result;
        }

        private IEnumerable<QueryBase> CreateWildcardQuery(string fieldName, string value, Func<String, String> preprocessFunc = null)
        {
            if (string.IsNullOrWhiteSpace((string)value))
            {
                yield break;
            }

            if (preprocessFunc != null)
            {
                value = preprocessFunc(value);
            }

            var result = new WildcardQuery
            {
                Field = fieldName,
                Value = value,
            };

            yield return result;
        }

        private IEnumerable<QueryBase> CreateRegexQuery(string fieldName, string value, Func<String, String> preprocessFunc = null)
        {
            if (string.IsNullOrWhiteSpace((string)value))
            {
                yield break;
            }

            if (preprocessFunc != null)
            {
                value = preprocessFunc(value);
            }

            var result = new RegexpQuery
            {
                Field = fieldName,
                Value = value,
            };

            yield return result;
        }
    }
}