using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Guards;
using PoeShared.Common;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;
using TypeConverter;

namespace PoeShared.Converters
{
    public class PoeStashItemToPoeItem : IConverter<IStashItem, IPoeItem>, IConverter<IStashItem, PoeItem>
    {
        private readonly IClock clock;
        private readonly StringToPoePriceConverter stringToPoePriceConverter = new StringToPoePriceConverter();

        public PoeStashItemToPoeItem(IClock clock)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));

            this.clock = clock;
        }

        IPoeItem IConverter<IStashItem, IPoeItem>.Convert(IStashItem value)
        {
            return Convert(value);
        }

        public PoeItem Convert(IStashItem value)
        {
            Guard.ArgumentNotNull(value, nameof(value));

            var result = new PoeItem();
            result.TabName = value.InventoryId;
            result.ItemName = $"{value.Name} {value.TypeLine}".Trim();
            result.ItemIconUri = value.Icon;
            result.Rarity = value.Rarity;
            result.ItemLevel = value.ItemLevel > 0 ? value.ItemLevel.ToString() : null;
            result.Hash = value.Id;
            result.League = value.League;
            
            result.Modifications |= PoeItemModificatins.None;
            result.Modifications |= value.Corrupted ? PoeItemModificatins.Corrupted : PoeItemModificatins.None;
            result.Modifications |= !value.Identified ? PoeItemModificatins.Unidentified : PoeItemModificatins.None;
            result.Modifications |= value.CraftedMods.EmptyIfNull().Any() ? PoeItemModificatins.Crafted : PoeItemModificatins.None;
            
            result.Note = value.Note;
            result.Timestamp = clock.Now;

            var itemPrice = string.IsNullOrWhiteSpace(value.Note)
                ? PoePrice.Empty
                : stringToPoePriceConverter.Convert(value.Note);
            result.Price = !itemPrice.IsEmpty
                ? itemPrice.ToString()
                : null;

            var mods = new List<IPoeItemMod>();
            foreach (var valueImplicitMod in value.ImplicitMods.EmptyIfNull())
            {
                var mod = new PoeItemMod
                {
                    Name = valueImplicitMod,
                    ModType = PoeModType.Implicit
                };
                mods.Add(mod);
            }

            foreach (var valueExplicitMod in value.ExplicitMods.EmptyIfNull())
            {
                var mod = new PoeItemMod
                {
                    Name = valueExplicitMod,
                    ModType = PoeModType.Explicit
                };
                mods.Add(mod);
            }

            foreach (var valueCraftedMod in value.CraftedMods.EmptyIfNull())
            {
                var mod = new PoeItemMod
                {
                    Name = valueCraftedMod,
                    ModType = PoeModType.Explicit,
                    Origin = PoeModOrigin.Craft
                };
                mods.Add(mod);
            }

            result.Mods = mods.ToArray();

            VisitExtendedInfo(value.Extended, result);
            VisitProperties(value.Properties, result);
            VisitSockets(value.Sockets, result);
            VisitRequirements(value.Requirements, result);

            result.Raw = $"{value.DumpToText()}\n\nConverted\n\n{result.DumpToText()}";
            return result;
        }

        private void VisitRequirements(IEnumerable<StashItemProperty> source, PoeItem result)
        {
            if (source == null)
            {
                return;
            }

            var stats = source.Select(x => $"{x.Name} {string.Join(" ", x.Values.Where(value => value.IsValid).Select(value => value.ToDisplayValue()))}");
            result.Requirements = string.Join(" ", stats);
        }

        private void VisitSockets(IEnumerable<StashItemSocket> source, PoeItem result)
        {
            if (source == null)
            {
                return;
            }
            
            var groupsById = new ConcurrentDictionary<int, string>();
            source.ForEach(socket => groupsById.AddOrUpdate(socket.Group, socket.Colour, (i, s) => s + "-" + socket.Colour));

            var raw = string.Join("", groupsById.Values);
            result.Links = new PoeLinksInfo(raw);
        }


        private void VisitExtendedInfo(StashItemExtendedInfo source, PoeItem result)
        {
            if (source == null)
            {
                return;
            }

            result.PhysicalDamagePerSecond = source.Pdps == null ? null : source.Pdps.ToString();
            result.ElementalDamagePerSecond = source.Edps == null ? null : source.Edps.ToString();
            result.DamagePerSecond = source.Pdps == null || source.Edps == null ? null : (source.Pdps + source.Edps).ToString();
        }

        private void VisitProperties(IEnumerable<StashItemProperty> sourceRaw, PoeItem result)
        {
            if (sourceRaw == null)
            {
                return;
            }
            var sourceByName = new Dictionary<string, StashItemProperty>(StringComparer.OrdinalIgnoreCase);
            sourceRaw.Where(x => !string.IsNullOrEmpty(x.Name)).ForEach(x =>
            {
                if (sourceByName.TryGetValue(x.Name, out var existingItem))
                {
                    Log.Instance.Warn($"[PoeStashItemToPoeItem] Properties dictionary already contains key {x.Name}, existing entry: {existingItem}, new: {x}");
                    return;
                }

                sourceByName[x.Name] = x;
            });
            result.AttacksPerSecond = result.AttacksPerSecond ?? ExtractProperty("Attacks per Second", sourceByName);
            result.CriticalChance = result.CriticalChance ?? ExtractProperty("Critical Strike Chance", sourceByName);
            result.ItemLevel = result.ItemLevel ?? ExtractProperty("Level", sourceByName);
            result.Quality = result.Quality ?? ExtractProperty("Quality", sourceByName);
            result.Evasion = result.Evasion ?? ExtractProperty("Evasion Rating", sourceByName);
            result.Shield = result.Shield ?? ExtractProperty("Energy Shield", sourceByName);
            result.Armour = result.Armour ?? ExtractProperty("Armour", sourceByName);
            result.BlockChance = result.BlockChance ?? ExtractProperty("Chance to Block", sourceByName);
        }

        private string ExtractProperty(string key, IDictionary<string, StashItemProperty> propertiesByName)
        {
            string result = null;
            if (propertiesByName.TryGetValue(key, out var property) && 
                property.Values?.Count >= 1 &&
                property.Values[0].IsValid)
            {
                result = property.Values[0].Min;
            }
            return result;
        }
    }
}