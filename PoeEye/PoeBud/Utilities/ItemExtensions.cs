﻿using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.Utilities
{
    using System;
    using System.Linq;

    internal static class ItemExtensions
    {
        public static bool IsWeapon(this GearType gearType)
        {
            switch (gearType)
            {
                case GearType.Axe:
                case GearType.Claw:
                case GearType.Bow:
                case GearType.Quiver:
                case GearType.Sceptre:
                case GearType.Staff:
                case GearType.Sword:
                case GearType.Shield:
                case GearType.Dagger:
                case GearType.Mace:
                case GearType.Wand:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsWeapon(this IItem item)
        {
            return item.ItemType.IsWeapon();
        }

        public static float GetTradeScore(this IItem item)
        {
            if (item.IsWeapon())
            {
                if (item.W >= 2 && item.H >= 4)
                {
                    return 1;
                }
                else if (item.ItemType == GearType.Bow)
                {
                    return 1;
                }else
                {
                    return 0.5f;
                };
            }
            else
            {
                switch (item.ItemType)
                {
                    case GearType.Ring:
                        return 0.5f;
                    case GearType.Amulet:
                    case GearType.Helmet:
                    case GearType.Chest:
                    case GearType.Belt:
                    case GearType.Gloves:
                    case GearType.Boots:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        public static int? GetTabIndex(this IItem item)
        {
            if (string.IsNullOrWhiteSpace(item.InventoryId))
            {
                return null;
            }

            var idx = item.InventoryId.Replace("Stash", string.Empty);

            int result;
            if (!int.TryParse(idx, out result))
            {
                return null;
            }
            return result - 1; // PoE Stash Idx (left-most tab) = Stash1 
        }

        public static IItem[] GetTradeableItems(this IItem[] items)
        {
            return items
                .Where(x => x.Rarity == PoeItemRarity.Rare)
                .Where(x => x.SocketedItems == null || !x.SocketedItems.Any())
                .Where(x => x.Sockets == null || x.Sockets.Count < 6)
                .Where(x => x.GetTabIndex() != null)
                .ToArray();
        }
    }
}