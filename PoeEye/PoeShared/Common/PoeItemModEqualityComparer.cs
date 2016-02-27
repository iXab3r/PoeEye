namespace PoeShared.Common
{
    using System.Collections;
    using System.Collections.Generic;

    public sealed class PoeItemModEqualityComparer : IEqualityComparer<IPoeItemMod>, IComparer
    {
        public int Compare(object x, object y)
        {
            if (x is IPoeItemMod && y is IPoeItemMod)
            {
                return Equals(x as IPoeItemMod, y as IPoeItemMod) ? 0 : 1;
            }
            if (x is IPoeItemMod)
            {
                return 1;
            }
            return -1;
        }

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
            return obj.Name != null ? obj.Name.GetHashCode() : 0;
        }
    }
}