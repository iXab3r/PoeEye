using System.Linq;
using PoeShared.Common;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Scaffolding
{
    public static class ItemExtensions
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

        public static bool IsWeapon(this IStashItem item)
        {
            return item.ItemType.IsWeapon();
        }

        public static float GetTradeScore(this IStashItem item)
        {
            if (item.IsWeapon())
            {
                if (item.Width >= 2 && item.Height >= 4)
                {
                    return 1;
                }

                if (item.ItemType == GearType.Bow)
                {
                    return 1;
                }

                return 0.5f;
                ;
            }

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

        public static int? GetTabIndex(this IStashItem item)
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

        public static string GetInventoryId(this IStashTab tab)
        {
            return $"Stash{tab.Idx + 1}";
        }

        public static IStashItem[] GetChaosSetItems(this IStashItem[] items)
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