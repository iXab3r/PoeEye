using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Guards;
using Newtonsoft.Json.Linq;
using PoeEye.ExileToolsApi.Entities;
using PoeEye.ExileToolsApi.Extensions;
using PoeShared.Common;
using TypeConverter;
using static System.String;

namespace PoeEye.ExileToolsApi.Converters
{
    internal class ToPoeItemConverter : IConverter<ItemConversionInfo, IPoeItem>
    {
        public IPoeItem Convert(ItemConversionInfo conversionInfo)
        {
            var value = conversionInfo.Item;

            var result = new PoeItem()
            {
                ItemName = value.Info?.FullName,
                DamagePerSecond = GetOrDefaultAsString(value.PropertiesPseudo?.Weapon?.Q20?.TotalDps),
                PhysicalDamagePerSecond = GetOrDefaultAsString(value.PropertiesPseudo?.Weapon?.Q20?.PhysicalDps),
                ElementalDamagePerSecond = GetOrDefaultAsString(value.Properties?.Weapon?.ElementalDps),

                Armour = GetOrDefaultAsString(value.PropertiesPseudo?.Armour?.Q20?.Armour),
                Evasion = GetOrDefaultAsString(value.PropertiesPseudo?.Armour?.Q20?.Evasion),
                Shield = GetOrDefaultAsString(value.PropertiesPseudo?.Armour?.Q20?.EnergyShield),

                CriticalChance = GetOrDefaultAsString(value.Properties?.Weapon?.CriticalStrikeChance),
                AttacksPerSecond = GetOrDefaultAsString(value.Properties?.Weapon?.AttacksPerSecond),
                Elemental = GetOrDefaultAsString(value.Properties?.Weapon?.ElementalDamage),
                Physical = GetOrDefaultAsString(value.Properties?.Weapon?.PhysicalDamage),
                IsCorrupted = value.Attributes?.IsCorrupted != null && (bool)value.Attributes?.IsCorrupted,
                IsUnidentified = value.Attributes?.IsIdentified != null && !(bool)value.Attributes?.IsIdentified,
                IsMirrored = value.Attributes?.IsMirrored != null && (bool)value.Attributes?.IsMirrored,
                ItemIconUri = value.Info?.Icon,
                UserIgn = value.Shop?.LastCharacterName,
                UserForumName = value.Shop?.SellerAccount,
                Hash = value.ItemId,
                UserIsOnline = value.Shop?.Status != null && value.Shop?.Status == VerificationStatus.Yes,
                Rarity = GetOrDefaultEnum(value.Attributes?.Rarity, PoeItemRarity.Normal),
                League = value.Attributes?.League,
                Note = value.Shop?.Note,
                FirstSeen = value.Shop?.AddedTimestamp,
                Quality = GetOrDefault(value.Properties?.Quality).ToString(CultureInfo.InvariantCulture),
                BlockChance = GetOrDefaultAsString(value.Properties?.Armour?.BlockChance),
                Links = value.Sockets == null ? null : new PoeLinksInfo(value.Sockets.Raw),
            };

            if (GetOrDefaultEnum(value.Attributes?.ItemType, KnownItemType.Unknown) == KnownItemType.Gem)
            {
                result.Level = GetOrDefaultAsString(value.Properties?.Gem?.Level);
            }

            if (GetOrDefault(value.Shop?.HasPrice) && !IsNullOrWhiteSpace(value.Shop?.CurrencyRequested))
            {
                result.Price = $"{value.Shop?.AmountRequested} {value.Shop?.CurrencyRequested}";
            }
            else
            {
                result.Price = $"{value.Shop.Note}";
            }

            result.SuggestedPrivateMessage = value.Shop?.DefaultMessage;


            result.Requirements = Join(" ",
                GetWithNameOrDefault(value.Requirements?.Level, "Lvl"),
                GetWithNameOrDefault(value.Requirements?.Strength, "Str"),
                GetWithNameOrDefault(value.Requirements?.Dexterity, "Dex"),
                GetWithNameOrDefault(value.Requirements?.Intelligence, "Int"));


            var itemMods = new List<IPoeItemMod>();
            foreach (var modsList in (value.Mods ?? new Dictionary<string, ExTzItemMods>()).Select(x => x.Value))
            {
                itemMods.AddRange(from mod in modsList.Explicit ?? new Dictionary<string, object>() select ToPoeItemMod(mod.Key, mod.Value, PoeModType.Explicit, false));
                itemMods.AddRange(from mod in modsList.Implicit ?? new Dictionary<string, object>() select ToPoeItemMod(mod.Key, mod.Value, PoeModType.Implicit, false));
            }

            var additionalMods = from mod in value.ModsPseudo ?? new Dictionary<string, object>()
                                 where conversionInfo.AdditionalModsToInclude.Any(x => x.IndexOf(mod.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                                 select ToPoeItemMod($"[pseudo] {mod.Key}", mod.Value, PoeModType.Unknown, true);
            itemMods.AddRange(additionalMods);

            result.Mods = itemMods.ToArray();

            return result;
        }

        private IPoeItemMod ToPoeItemMod(string codeName, object objectValue, PoeModType modType, bool isCrafted)
        {
            var modName = codeName;

            if (objectValue is double || objectValue is int || objectValue is float || objectValue is long || objectValue is decimal)
            {
                modName = ToModName(codeName, objectValue);
            }
            else if (objectValue is JObject)
            {
                var jsonValue = objectValue as JObject;
                ExTzPropertyRange rangeValue;
                if (jsonValue.ToString().TryParseJson(out rangeValue))
                {
                    modName = ToModName(codeName, rangeValue.Min, rangeValue.Max);
                }
                else
                {
                    modName = ToModName(codeName, objectValue);
                }
            }

            return new PoeItemMod()
            {
                CodeName = codeName,
                Name = modName,
                IsCrafted = isCrafted,
                ModType = modType,
            };
        }

        private string ToModName(string codeName, params object[] value)
        {
            var nextIdx = 0;
            var result = new StringBuilder();
            var valueIdx = 0;
            while ((nextIdx = codeName.IndexOf("#", StringComparison.Ordinal)) >= 0)
            {
                var leftPart = codeName.Substring(0, nextIdx);
                codeName = codeName.Substring(nextIdx + 1);

                result.Append(leftPart);
                if (value.Length > valueIdx)
                {
                    result.Append(value[valueIdx++]);
                }
            }

            result.Append(codeName);
            return result.ToString();
        }

        private string GetWithNameOrDefault<T>(T? container, string name)
            where T : struct
        {
            return GetWithNameOrDefault(container, x => $"{name}: {x}");
        }

        private string GetWithNameOrDefault<T>(T? container, Func<T, string> converter) where T : struct
        {
            return container == null || default(T).Equals(container.Value) ? Empty : converter(container.Value);
        }

        private string GetOrDefaultAsString<T>(T? container) where T : struct
        {
            return container == null || default(T).Equals(container.Value) ? Empty : container.Value.ToString();
        }

        private T GetOrDefaultEnum<T>(string container, T defaultValue = default(T)) where T : struct
        {
            T result;
            if (Enum.TryParse(container, out result))
            {
                return result;
            }
            return defaultValue;
        }

        private T GetOrDefault<T>(T? container, T defaultValue = default(T)) where T : struct
        {
            return container ?? defaultValue;
        }
    }
}