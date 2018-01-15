using System.Collections.Generic;
using System.Linq;
using System.Text;
using Guards;
using PoeShared.Common;
using PoeShared.Prism;
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
            result.ItemLevel = value.ItemLevel.ToString();
            result.Hash = value.Id;
            result.League = value.League;
            result.Modifications |= PoeItemModificatins.Corrupted;
            result.Modifications |= !value.Identified ? PoeItemModificatins.Unidentified : PoeItemModificatins.None;
            result.Note = value.Note;
            result.Timestamp = clock.Now;

            var itemPrice = string.IsNullOrWhiteSpace(value.Note)
                ? PoePrice.Empty 
                : stringToPoePriceConverter.Convert(value.Note);
            result.Price = !itemPrice.IsEmpty
                ? itemPrice.ToString()
                : null;

            var requirementsBuilder =
                value.Requirements
                    .EmptyIfNull()
                    .Select(FormatRequirement)
                    .ToArray();
            result.Requirements = string.Join(", ", requirementsBuilder);

            var mods = new List<IPoeItemMod>();
            foreach (var valueImplicitMod in value.ImplicitMods.EmptyIfNull())
            {
                var mod = new PoeItemMod()
                {
                    Name = valueImplicitMod,
                    ModType = PoeModType.Implicit
                };
                mods.Add(mod);
            }
            foreach (var valueExplicitMod in value.ExplicitMods.EmptyIfNull())
            {
                var mod = new PoeItemMod()
                {
                    Name = valueExplicitMod,
                    ModType = PoeModType.Explicit
                };
                mods.Add(mod);
            }
            foreach (var valueCraftedMod in value.CraftedMods.EmptyIfNull())
            {
                var mod = new PoeItemMod()
                {
                    Name = valueCraftedMod,
                    ModType = PoeModType.Explicit,
                    Origin = PoeModOrigin.Craft,
                };
                mods.Add(mod);
            }
            result.Mods = mods.ToArray();

            return result;
        }

        private string FormatRequirement(StashItemRequirement requirement)
        {
            if (requirement == null || string.IsNullOrWhiteSpace(requirement.Name))
            {
                return string.Empty;
            }

            var values = requirement.Values.EmptyIfNull()
                .Where(x => x != null)
                .Where(IsValidRequirementValue)
                .ToArray();

            return $"{requirement.Name} {string.Join(" ", values)}";
        }

        private bool IsValidRequirementValue(object value)
        {
            if (value is string)
            {
                return !string.IsNullOrWhiteSpace(value as string);
            }
            if (value is int)
            {
                var intValue = (int)value;
                return intValue > 0;
            }
            return false;
        }
    }
}
