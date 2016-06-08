using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Guards;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeEye.ExileToolsApi.Entities;
using PoeEye.ExileToolsApi.Extensions;
using PoeShared;
using PoeShared.Common;
using TypeConverter;
using static System.String;

namespace PoeEye.ExileToolsApi.Converters
{
    internal class ToPoeItemConverter : IConverter<ItemConversionInfo, IPoeItem>
    {
        public IPoeItem Convert(ItemConversionInfo conversionInfo)
        {
            var rawJson = conversionInfo.Item.ToString(Formatting.Indented); 

            ExTzItem source;
            try
            {
                source = JsonConvert.DeserializeObject<ExTzItem>(rawJson);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Error occurred during deserialization process, value:\r\n{rawJson}", ex);
                return new PoeItem();
            }
            
            var result = new PoeItem
            {
                Raw = rawJson.FormatJson(),
                ItemName = source.Info?.FullName,
                DamagePerSecond = GetOrDefaultAsString(source.ItemPropertiesPseudo?.Weapon?.Q20?.TotalDps),
                PhysicalDamagePerSecond = GetOrDefaultAsString(source.ItemPropertiesPseudo?.Weapon?.Q20?.PhysicalDps),
                ElementalDamagePerSecond = GetOrDefaultAsString(source.ItemProperties?.Weapon?.ElementalDps),
                Armour = GetOrDefaultAsString(source.ItemPropertiesPseudo?.Armour?.Q20?.Armour),
                Evasion = GetOrDefaultAsString(source.ItemPropertiesPseudo?.Armour?.Q20?.Evasion),
                Shield = GetOrDefaultAsString(source.ItemPropertiesPseudo?.Armour?.Q20?.EnergyShield),
                CriticalChance = GetOrDefaultAsString(source.ItemProperties?.Weapon?.CriticalStrikeChance),
                AttacksPerSecond = GetOrDefaultAsString(source.ItemProperties?.Weapon?.AttacksPerSecond),
                Elemental = GetOrDefaultAsString(source.ItemProperties?.Weapon?.ElementalDamage),
                Physical = GetOrDefaultAsString(source.ItemProperties?.Weapon?.PhysicalDamage),
                IsCorrupted = source.Attributes?.IsCorrupted != null && (bool) source.Attributes?.IsCorrupted,
                IsUnidentified = source.Attributes?.IsIdentified != null && !(bool) source.Attributes?.IsIdentified,
                IsMirrored = source.Attributes?.IsMirrored != null && (bool) source.Attributes?.IsMirrored,
                ItemIconUri = source.Info?.Icon,
                UserIgn = source.Shop?.LastCharacterName,
                UserForumName = source.Shop?.SellerAccount,
                Hash = source.ItemId,
                UserIsOnline = source.Shop?.Status != null && source.Shop?.Status == VerificationStatus.Yes,
                Rarity = GetOrDefaultEnum(source.Attributes?.Rarity, PoeItemRarity.Normal),
                League = source.Attributes?.League,
                Note = source.Shop?.Note,
                FirstSeen = source.Shop?.AddedTimestamp,
                Quality = GetOrDefault(source.ItemProperties?.Quality).ToString(CultureInfo.InvariantCulture),
                BlockChance = GetOrDefaultAsString(source.ItemProperties?.Armour?.BlockChance),
                Links = source.Sockets == null ? null : new PoeLinksInfo(source.Sockets.Raw),
            };


            if (GetOrDefaultEnum(source.Attributes?.ItemType, KnownItemType.Unknown) == KnownItemType.Gem)
            {
                result.Level = GetOrDefaultAsString(source.ItemProperties?.Gem?.Level);
            }

            if (GetOrDefault(source.Shop?.HasPrice) && !IsNullOrWhiteSpace(source.Shop?.CurrencyRequested))
            {
                result.Price = $"{source.Shop?.AmountRequested} {source.Shop?.CurrencyRequested}";
            }
            else
            {
                result.Price = $"{source.Shop.Note}";
            }

            result.SuggestedPrivateMessage = source.Shop?.DefaultMessage;


            result.Requirements = Join(" ",
                GetWithNameOrDefault(source.Requirements?.Level, "Lvl"),
                GetWithNameOrDefault(source.Requirements?.Strength, "Str"),
                GetWithNameOrDefault(source.Requirements?.Dexterity, "Dex"),
                GetWithNameOrDefault(source.Requirements?.Intelligence, "Int"));

            var itemMods = new List<IPoeItemMod>();

            var enchants = from mod in source.EnchantMods ?? new Dictionary<string, object>()
                           select ToPoeItemMod($"[enchant] {mod.Key}", mod.Value, PoeModType.Unknown, true);
            itemMods.AddRange(enchants);

            foreach (var modsList in (source.Mods ?? new Dictionary<string, ExTzItemMods>()).Select(x => x.Value))
            {
                itemMods.AddRange(from mod in modsList.Explicit ?? new Dictionary<string, object>() select ToPoeItemMod(mod.Key, mod.Value, PoeModType.Explicit, false));
                itemMods.AddRange(from mod in modsList.Implicit ?? new Dictionary<string, object>() select ToPoeItemMod(mod.Key, mod.Value, PoeModType.Implicit, false));
                itemMods.AddRange(from mod in modsList.Crafted ?? new Dictionary<string, object>() select ToPoeItemMod($"[craft] {mod.Key}", mod.Value, PoeModType.Unknown, true));
            }

            var additionalMods = from mod in source.ModsPseudo ?? new Dictionary<string, object>()
                                 where conversionInfo.AdditionalModsToInclude.Any(x => x.IndexOf(mod.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                                 select ToPoeItemMod($"[pseudo] {mod.Key}", mod.Value, PoeModType.Unknown, true);
            itemMods.AddRange(additionalMods);

            if (!string.IsNullOrWhiteSpace(source.Info?.ProphecyText))
            {
                var prophecyMod = new PoeItemMod()
                {
                    Name = source.Info.ProphecyText,
                    CodeName = "Prophecy",
                    IsCrafted = true,
                    ModType = PoeModType.Unknown,
                };
                itemMods.Add(prophecyMod);
            }

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