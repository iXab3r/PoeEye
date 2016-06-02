using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nest;
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
        public ISearchRequest Convert(IPoeQueryInfo source)
        {
            var result = new SearchRequest
            {
                Query = new FilteredQuery() { },
                From = 0,
                Size = 50,
            };

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
                CreateWildcardQuery("info.fullName", source.ItemName, x => $"*{x}*"),
                CreateTermQuery("attributes.league", source.League),
                CreateTermQuery("attributes.rarity", source.ItemRarity != null ? source.ItemRarity.ToString() : null),
                CreateTermQuery("shop.hasPrice", source.BuyoutOnly ? true : default(bool?)),
                CreateTermQuery("shop.sellerAccount", source.AccountName),
                CreateTermQuery("shop.verified", source.OnlineOnly ? VerificationStatus.Yes : default(VerificationStatus?)),
                CreateQuery(source.ItemType),
            };

            result.Query &= new BoolQuery
            {
                Must = CombineQueries(mustQueries)
            };

            result.Query &= PrepareSocketsColorQuery(source);
            result.Query &= PrepareSocketsLinksQuery(source);

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

        private IEnumerable<QueryBase> CreateQuery(string fieldName, float? min, float? max)
        {
            if (min == null && max == null)
            {
                yield break;
            }

            var queries = new List<string>();
            if (min != null)
            {
                queries.Add($"{fieldName}:>={min.Value}");
            }
            if (max != null)
            {
                queries.Add($"{fieldName}:<={max.Value}");
            }
            var result = new QueryStringQuery()
            {
                Query = string.Join(" ", queries),
            };
            yield return result;
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
                result.GreaterThanOrEqualTo = max.Value;
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

        private IEnumerable<QueryBase> CreateRegexQuery(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace((string)value))
            {
                yield break;
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