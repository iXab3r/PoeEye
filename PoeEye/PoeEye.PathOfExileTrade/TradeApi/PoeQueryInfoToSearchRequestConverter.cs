using System;
using System.Collections.Generic;
using System.Linq;
using PoeEye.PathOfExileTrade.TradeApi.Domain;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using TypeConverter;

namespace PoeEye.PathOfExileTrade.TradeApi
{
    internal sealed class PoeQueryInfoToSearchRequestConverter : IConverter<IPoeQueryInfo, JsonSearchRequest.Query>
    {
        public JsonSearchRequest.Query Convert(IPoeQueryInfo query)
        {
            var result = new JsonSearchRequest.Query
            {
                Term = query.ItemName
            };

            if (query.OnlineOnly)
            {
                result.Status = ToOptionValue("online");
            }

            var filters = result.Filters = new JsonSearchRequest.QueryFilters();
            VisitIfNeeded(x => filters.TradeFilters = x, new JsonSearchRequest.TradeFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.TradeFilterDef
                {
                    Price = ToPrice(query),
                    Account = ToAccount(query)
                }
            });

            VisitIfNeeded(x => filters.TypeFilters = x, new JsonSearchRequest.TypeFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.TypeFilterDef
                {
                    Category = ToOptionValue(query.ItemType?.CodeName),
                    Rarity = ToOptionValue(query.ItemRarity)
                }
            });

            VisitIfNeeded(x => filters.WeaponFilters = x, new JsonSearchRequest.WeaponFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.WeaponFilterDef
                {
                    Pdps = ToMinMaxValue(query.PdpsMin, query.PdpsMax),
                    Edps = ToMinMaxValue(query.EdpsMin, query.EdpsMax),
                    Aps = ToMinMaxValue(query.ApsMin, query.ApsMax),
                    Crit = ToMinMaxValue(query.CritMin, query.CritMax),
                    Damage = ToMinMaxValue(query.DamageMin, query.DamageMax),
                    Dps = ToMinMaxValue(query.DpsMin, query.DpsMax)
                }
            });

            VisitIfNeeded(x => filters.ArmourFilters = x, new JsonSearchRequest.ArmourFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.ArmourFilterDef
                {
                    Armour = ToMinMaxValue(query.ArmourMin, query.ArmourMax),
                    Block = ToMinMaxValue(query.BlockMin, query.BlockMax),
                    Evasion = ToMinMaxValue(query.EvasionMin, query.EvasionMax),
                    EnergyShield = ToMinMaxValue(query.ShieldMin, query.ShieldMax)
                }
            });

            VisitIfNeeded(x => filters.ReqFilters = x, new JsonSearchRequest.ReqFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.RequirementsFilterDef
                {
                    Lvl = ToMinMaxValue(query.LevelMin, query.LevelMax),
                    Dex = ToMinMaxValue(query.RDexMin, query.RDexMax),
                    Int = ToMinMaxValue(query.RIntMin, query.RIntMax),
                    Str = ToMinMaxValue(query.RStrMin, query.RStrMax)
                }
            });
            
            VisitIfNeeded(x => filters.SocketFilters = x, new JsonSearchRequest.SocketFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.SocketFilterDef
                {
                    Links = ToSocketDef(
                        query.LinkMin,
                        query.LinkMax,
                        query.LinkedR,
                        query.LinkedG,
                        query.LinkedB,
                        query.LinkedW
                    ),
                    Sockets = ToSocketDef(
                       query.SocketsMin,
                       query.SocketsMax,
                       query.SocketsR,
                       query.SocketsG,
                       query.SocketsB,
                       query.SocketsW
                    ),
                }
            });

            VisitIfNeeded(x => filters.MiscFilters = x, new JsonSearchRequest.MiscFilters
            {
                Disabled = false,
                Value = new JsonSearchRequest.MiscFilterDef
                {
                    Quality = ToMinMaxValue(query.QualityMin, query.QualityMax),
                    GemLevel = ToMinMaxValue(query.LevelMin, query.LevelMax),
                    Corrupted = ToOptionValue(query.CorruptionState),
                    Crafted = ToOptionValue(query.CraftState),
                    Enchanted = ToOptionValue(query.EnchantState),
                    ElderItem = ToOptionValue(query.AffectedByElderState),
                    ShaperItem = ToOptionValue(query.AffectedByShaperState),
                    Ilvl = ToMinMaxValue(query.ItemLevelMin, query.ItemLevelMax)
                }
            });

            result.Groups = ConvertMods(query).ToArray();

            return result;
        }

        private void VisitIfNeeded<T>(Action<T> setter, T value) where T : JsonSearchRequest.BaseFilter
        {
            if (!IsConfigured(value))
            {
                return;
            }

            setter(value);
        }

        private bool IsConfigured(JsonSearchRequest.BaseFilterDef def)
        {
            var properties = def.GetType().GetProperties()
                                .Where(x => x.CanRead && x.CanWrite)
                                .Where(x => x.PropertyType.IsClass || x.PropertyType == typeof(long?) || x.PropertyType == typeof(int?))
                                .ToArray();
            var values = properties.Select(x => x.GetValue(def)).ToArray();

            return values.Any(x =>
            {
                if (x is JsonSearchRequest.BaseFilterDef)
                {
                    return IsConfigured(x as JsonSearchRequest.BaseFilterDef);
                }

                if (x is long?)
                {
                    return true;
                } 
                else if (x is int?)
                {
                    return true;
                }

                return !ReferenceEquals(null, x);
            });
        }

        private JsonSearchRequest.SocketsDef ToSocketDef(long? min, long? max, long? red, long? green, long? blue, long? white)
        {
            var result = new JsonSearchRequest.SocketsDef()
            {
                Min = min,
                Max = max,
                R = red,
                G = green,
                B = blue,
                W = white
            };
            if (!IsConfigured(result))
            {
                return null;
            }

            return result;
        }

        private IEnumerable<JsonSearchRequest.FilterGroup> ConvertMods(IPoeQueryInfo query)
        {
            if (query.ModGroups.Length == 0)
            {
                yield return new JsonSearchRequest.FilterGroup
                {
                    Type = "and",
                    Filters = new JsonSearchRequest.Filter[] { }
                };
            }

            foreach (var poeQueryModsGroup in query.ModGroups.Where(x => x.GroupType != PoeQueryModsGroupType.Unknown))
            {
                var filterGroup = new JsonSearchRequest.FilterGroup
                {
                    Type = ToStringType(poeQueryModsGroup.GroupType),
                    GroupValue = ToMinMaxValue(poeQueryModsGroup.Min, poeQueryModsGroup.Max),
                    Filters = poeQueryModsGroup.Mods.Where(x => !string.IsNullOrEmpty(x?.Name)).Select(ToFilter).ToArray()
                };
                yield return filterGroup;
            }
        }

        private JsonSearchRequest.Price ToPrice(IPoeQueryInfo query)
        {
            if (string.IsNullOrWhiteSpace(query.BuyoutCurrencyType))
            {
                return null;
            }

            if (query.BuyoutMax == null && query.BuyoutMin == null)
            {
                return null;
            }

            return new JsonSearchRequest.Price
            {
                Option = query.BuyoutCurrencyType,
                Min = ToLong(query.BuyoutMin),
                Max = ToLong(query.BuyoutMax)
            };
        }

        private JsonSearchRequest.Account ToAccount(IPoeQueryInfo query)
        {
            if (string.IsNullOrWhiteSpace(query.AccountName))
            {
                return null;
            }

            return new JsonSearchRequest.Account
            {
                Input = query.AccountName
            };
        }

        private JsonSearchRequest.Filter ToFilter(IPoeQueryRangeModArgument mod)
        {
            if (mod == null)
            {
                return null;
            }

            return new JsonSearchRequest.Filter
            {
                Id = mod.Mod.CodeName,
                Disabled = false,
                Value = ToMinMaxValue(mod.Min, mod.Max)
            };
        }

        private JsonSearchRequest.MinMaxValue ToMinMaxValue(float? min, float? max)
        {
            if (min == null && max == null)
            {
                return null;
            }

            return new JsonSearchRequest.MinMaxValue
            {
                Min = ToLong(min),
                Max = ToLong(max)
            };
        }

        private long? ToLong(float? value)
        {
            return value == null ? default(long?) : (long)Math.Round(value.Value);
        }

        private JsonSearchRequest.OptionValue ToOptionValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return new JsonSearchRequest.OptionValue
            {
                Option = value
            };
        }

        private JsonSearchRequest.OptionValue ToOptionValue(TriState? value)
        {
            if (value == null || value == TriState.Unknown)
            {
                return null;
            }

            return ToOptionValue(value == TriState.Yes ? "true" : "false");
        }

        private JsonSearchRequest.OptionValue ToOptionValue(PoeItemRarity? value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value)
            {
                case PoeItemRarity.Normal:
                    return ToOptionValue("normal");
                case PoeItemRarity.Magic:
                    return ToOptionValue("magic");
                case PoeItemRarity.Rare:
                    return ToOptionValue("rare");
                case PoeItemRarity.Unique:
                    return ToOptionValue("unique");
                case PoeItemRarity.Relic:
                    return ToOptionValue("uniquefoil");
                default:
                    return null;
            }
        }

        private string ToStringType(PoeQueryModsGroupType groupType)
        {
            switch (groupType)
            {
                case PoeQueryModsGroupType.And:
                    return "and";
                case PoeQueryModsGroupType.Not:
                    return "not";
                case PoeQueryModsGroupType.Count:
                    return "count";
                case PoeQueryModsGroupType.Sum:
                    return "weight";
                case PoeQueryModsGroupType.If:
                    return "if";
                default:
                    return null;
            }
        }
    }
}