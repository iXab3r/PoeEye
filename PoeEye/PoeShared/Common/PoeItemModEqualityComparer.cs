namespace PoeShared.Common
{
    using System.Collections.Generic;

    public sealed class PoeItemModEqualityComparer : IEqualityComparer<IPoeItemMod>
    {
        public bool Equals(IPoeItemMod x, IPoeItemMod y)
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
            if (x.GetType() != y.GetType())
            {
                return false;
            }
            return string.Equals(x.Name, y.Name);
        }

        public int GetHashCode(IPoeItemMod obj)
        {
            return (obj.Name != null ? obj.Name.GetHashCode() : 0);
        }
    }
}