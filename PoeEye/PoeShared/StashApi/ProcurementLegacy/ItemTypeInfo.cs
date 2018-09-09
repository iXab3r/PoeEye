using PoeShared.StashApi.DataTypes;

namespace PoeShared.StashApi.ProcurementLegacy
{
    public struct ItemTypeInfo
    {
        public string ItemName { get; set; }

        public string ItemType { get; set; }

        public GearType GearType { get; set; }

        public bool Equals(ItemTypeInfo other)
        {
            return string.Equals(ItemName, other.ItemName) && string.Equals(ItemType, other.ItemType) && GearType == other.GearType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is ItemTypeInfo && Equals((ItemTypeInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ItemName != null ? ItemName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ItemType != null ? ItemType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)GearType;
                return hashCode;
            }
        }

        public static bool operator ==(ItemTypeInfo left, ItemTypeInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemTypeInfo left, ItemTypeInfo right)
        {
            return !left.Equals(right);
        }
    }
}