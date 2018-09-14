using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeShared.Common
{
    internal sealed class PoeItemEqualityComparer : IEqualityComparer<IPoeItem>
    {
        public static readonly IEqualityComparer<IPoeItem> Instance = new PoeItemEqualityComparer();
        
        private readonly PoeItemModEqualityComparer itemModEqualityComparer = new PoeItemModEqualityComparer();

        public bool Equals(IPoeItem x, IPoeItem y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            var result = string.Equals(x.Hash, y.Hash, StringComparison.InvariantCultureIgnoreCase) && 
                         string.Equals(x.ItemName, y.ItemName, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.ItemIconUri, y.ItemIconUri, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.TradeForumUri, y.TradeForumUri, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.UserForumUri, y.UserForumUri, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.UserForumName, y.UserForumName, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.Price, y.Price, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(x.League, y.League, StringComparison.InvariantCultureIgnoreCase);

            result &= x.Mods.SequenceEqual(y.Mods, itemModEqualityComparer);
            result &= x.Modifications == y.Modifications;
            result &= x.UserIsOnline == y.UserIsOnline;
            return result;
        }

        public int GetHashCode(IPoeItem obj)
        {
            unchecked
            {
                var hashCode = obj.ItemName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ItemName) : 0;
                hashCode = (hashCode * 397) ^ (obj.ItemIconUri != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ItemIconUri) : 0);
                hashCode = (hashCode * 397) ^ (obj.TradeForumUri != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TradeForumUri) : 0);
                hashCode = (hashCode * 397) ^ (obj.UserForumUri != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.UserForumUri) : 0);
                hashCode = (hashCode * 397) ^ (obj.UserForumName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.UserForumName) : 0);
                hashCode = (hashCode * 397) ^ (obj.Price != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Price) : 0);
                hashCode = (hashCode * 397) ^ (obj.League != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.League) : 0);

                return hashCode;
            }
        }
    }
}