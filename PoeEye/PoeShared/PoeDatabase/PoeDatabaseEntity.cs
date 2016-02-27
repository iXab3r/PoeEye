namespace PoeShared.PoeDatabase
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    /// <summary>
    ///     Generalizes every item in PoE
    /// </summary>
    public class PoeDatabaseEntity
    {
        public static IEqualityComparer<PoeDatabaseEntity> Comparer { get; } = new CategoryNameBaseEqualityComparer();

        public string Category { get; private set; }

        public string Name { get; private set; }

        public string Base { get; private set; }

        public virtual void Deserialize(XmlNode node)
        {
            Category = node.SelectSingleNode(@"Property[@id='Category']")?.InnerText;

            Name = node.SelectSingleNode(@"Property[@id='Name']")?.InnerText;

            Base = node.SelectSingleNode(@"Property[@id='Base']")?.InnerText;
        }

        protected bool Equals(PoeDatabaseEntity other)
        {
            return string.Equals(Category, other.Category) && string.Equals(Name, other.Name);
        }

        private sealed class CategoryNameBaseEqualityComparer : IEqualityComparer<PoeDatabaseEntity>
        {
            public bool Equals(PoeDatabaseEntity x, PoeDatabaseEntity y)
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
                return string.Equals(x.Category, y.Category, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Base, y.Base, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(PoeDatabaseEntity obj)
            {
                unchecked
                {
                    var hashCode = obj.Category != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Category) : 0;
                    hashCode = (hashCode * 397) ^ (obj.Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name) : 0);
                    hashCode = (hashCode * 397) ^ (obj.Base != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Base) : 0);
                    return hashCode;
                }
            }
        }
    }
}