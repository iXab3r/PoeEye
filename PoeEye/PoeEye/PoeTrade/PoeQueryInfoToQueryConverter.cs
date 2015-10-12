namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using TypeConverter;

    internal sealed class PoeQueryInfoToQueryConverter : IConverter<IPoeQueryInfo, IPoeQuery>
    {
        public IPoeQuery Convert(IPoeQueryInfo source)
        {
            Guard.ArgumentNotNull(() => source);

            var result = new PoeQuery();

            var args = new List<IPoeQueryArgument>
            {
                CreateArgument("dmg_min",  source.DamageMin),
                CreateArgument("dmg_max",  source.DamageMax),
                CreateArgument("aps_min",  source.ApsMin),
                CreateArgument("aps_max",  source.ApsMax),
                CreateArgument("crit_min", source.CritMin),
                CreateArgument("crit_max", source.CritMax),
                CreateArgument("dps_min",  source.DpsMin),
                CreateArgument("dps_max",  source.DpsMax),
                CreateArgument("edps_min", source. EdpsMin),
                CreateArgument("edps_max", source. EdpsMax),
                CreateArgument("pdps_min", source. PdpsMin),
                CreateArgument("pdps_max", source. PdpsMax),
                CreateArgument("armour_min", source. ArmourMin),
                CreateArgument("armour_max", source. ArmourMax),
                CreateArgument("evasion_min", source. EvasionMin),
                CreateArgument("evasion_max", source. EvasionMax),
                CreateArgument("shield_min", source. ShieldMin),
                CreateArgument("shield_max", source. ShieldMax),
                CreateArgument("block_min", source. BlockMin),
                CreateArgument("block_max", source. BlockMax),
                CreateArgument("sockets_min", source. SocketsMin),
                CreateArgument("sockets_max", source. SocketsMax),
                CreateArgument("link_min", source. LinkMin),
                CreateArgument("link_max", source. LinkMin),
                CreateArgument("rlevel_min", source. RLevelMin),
                CreateArgument("rlevel_max", source. RLevelMax),
                CreateArgument("rstr_min", source. RStrMin),
                CreateArgument("rstr_max", source. RStrMax),
                CreateArgument("rdex_min", source. RDexMin),
                CreateArgument("rdex_max", source. RDexMax),
                CreateArgument("rint_min", source. RIntMin),
                CreateArgument("rint_max", source. RIntMax),
                CreateArgument("q_min", source. QualityMin),
                CreateArgument("q_max", source. QualityMax),
                CreateArgument("level_min", source. LevelMin),
                CreateArgument("level_max", source. LevelMax),
                CreateArgument("mapq_min", source. IncQuantityMin),
                CreateArgument("mapq_max", source. IncQuantityMax),
                CreateArgument("buyout_min", source. BuyoutMin),
                CreateArgument("buyout_max", source. BuyoutMax),
                CreateArgument("buyout_currency", source.BuyoutCurrencyType),
                CreateArgument("buyout", source. BuyoutOnly),
                CreateArgument("online", source. OnlineOnly),
                CreateArgument("altart", source. AlternativeArt),
                CreateArgument("capquality", source. NormalizeQuality),
                CreateArgument("base", source. ItemBase),
                CreateArgument("name", source. ItemName),
                CreateArgument("league", source. League),
                CreateArgument("sockets_r", source. SocketsR),
                CreateArgument("sockets_g", source. SocketsG),
                CreateArgument("sockets_b", source. SocketsB),
                CreateArgument("sockets_w", source. SocketsW),
                CreateArgument("linked_r", source. LinkedR),
                CreateArgument("linked_g", source. LinkedG),
                CreateArgument("linked_b", source. LinkedB),
                CreateArgument("linked_w", source. LinkedW),
                CreateArgument("type", source. ItemType?.CodeName),
                CreateArgument("rarity", source. ItemRarity?.ToString().ToLowerInvariant() ?? string.Empty),
            };

            if (!string.IsNullOrWhiteSpace(source.ImplicitMod?.Name))
            {
                args.AddRange(new[]
                {
                    CreateArgument("impl", source.ImplicitMod.Name),
                    CreateArgument("impl_min", source.ImplicitMod.Min),
                    CreateArgument("impl_max", source.ImplicitMod.Max)
                });
            }

            args.AddRange(new[]
            {
                CreateArgument("mods", string.Empty),
                CreateArgument("modexclude", string.Empty),
                CreateArgument("modmin", string.Empty),
                CreateArgument("modmax", string.Empty)
            });

            foreach (var poeExplicitModViewModel in (source.ExplicitMods??new IPoeQueryRangeModArgument[0]).Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var modArg = CreateModArgument(
                    poeExplicitModViewModel.Name,
                    poeExplicitModViewModel.Min,
                    poeExplicitModViewModel.Max);
                args.Add(modArg);
            }

            // verify, that there are no duplicates
            Guard.ArgumentIsTrue(() => args.ToDictionary(x => x.Name, x => default(int?)).Count() == args.Count());

            result.Arguments = args.ToArray();
            return result;
        }

        private IPoeQueryArgument CreateModArgument(string modName, float? min, float? max)
        {
            var arg = new PoeQueryRangeModArgument(modName)
            {
                Min = min,
                Max = max
            };
            return arg;
        }

        private IPoeQueryArgument CreateModArgument(IPoeItemMod mod, float? min, float? max)
        {
            return CreateModArgument(mod.CodeName, min, max);
        }

        private IPoeQueryArgument CreateArgument<T>(string name, T value)
        {
            if (value == null || Equals(value, default(T)))
            {
                return new PoeQueryStringArgument(name, string.Empty);
            }

            if (typeof(T) == typeof(int?))
            {
                return new PoeQueryIntArgument(name,
                    value is int ? ConvertToType<int>(value) : (int)ConvertToType<int?>(value));
            }
            if (typeof(T) == typeof(float?))
            {
                return new PoeQueryFloatArgument(name,
                    value is float ? ConvertToType<float>(value) : (float)ConvertToType<float?>(value));
            }
            if (typeof(T) == typeof(string))
            {
                return new PoeQueryStringArgument(name, ConvertToType<string>(value) ?? string.Empty);
            }
            if (typeof(T) == typeof(bool))
            {
                return new PoeQueryStringArgument(name, ConvertToType<bool>(value) ? "x" : string.Empty);
            }
            throw new NotSupportedException($"Type {typeof(T)} is not supported, parameter name: {name}");
        }

        private T ConvertToType<T>(object value)
        {
            return (T)System.Convert.ChangeType(value, typeof(T));
        }
    }
}