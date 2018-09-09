using System;
using System.Collections.Generic;
using System.Linq;
using Guards;
using PoeShared.Common;
using Unity.Interception.Utilities;

namespace PoeEye.ItemParser.Services
{
    internal sealed class PoeItemSerializer : IPoeItemSerializer
    {
        private const string BlocksSeparator = "--------";

        public string Serialize(IPoeItem item)
        {
            Guard.ArgumentNotNull(item, nameof(item));

            var lines = new List<string>();

            void AppendBlockSeparator() => lines.Add(BlocksSeparator);

            if (item.Rarity != PoeItemRarity.Unknown)
            {
                lines.Add($"Rarity: {item.Rarity}");
            }

            if (!string.IsNullOrWhiteSpace(item.TypeInfo.ItemName) && !string.IsNullOrWhiteSpace(item.TypeInfo.ItemType))
            {
                lines.Add(item.TypeInfo.ItemName);
                lines.Add(item.TypeInfo.ItemType);
            }
            else if (!string.IsNullOrEmpty(item.ItemName))
            {
                lines.Add(item.ItemName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(item.TypeInfo.ItemName))
                {
                    lines.Add(item.TypeInfo.ItemName);
                }

                if (!string.IsNullOrWhiteSpace(item.TypeInfo.ItemType))
                {
                    lines.Add(item.TypeInfo.ItemType);
                }
            }

            var implicitMods = item.Mods
                                   .Where(x => x.ModType == PoeModType.Implicit)
                                   .Select(FormatName)
                                   .JoinStrings(Environment.NewLine);

            if (!string.IsNullOrEmpty(implicitMods))
            {
                AppendBlockSeparator();
                lines.Add(implicitMods);
            }

            var explicitMods = item.Mods
                                   .Where(x => x.ModType == PoeModType.Explicit)
                                   .Select(FormatName)
                                   .JoinStrings(Environment.NewLine);
            if (!string.IsNullOrEmpty(explicitMods))
            {
                AppendBlockSeparator();
                lines.Add(explicitMods);
            }

            return lines.JoinStrings(Environment.NewLine);
        }

        private string FormatName(IPoeItemMod mod)
        {
            return $"{(mod.Origin == PoeModOrigin.Craft || mod.Origin == PoeModOrigin.Enchant ? "{crafted}" : string.Empty)}{mod.Name}";
        }
    }
}