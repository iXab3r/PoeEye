namespace PoeShared.PoeDatabase
{
    using System.Xml;

    /// <summary>
    /// Generalizes every item in PoE
    /// </summary>
    public class PoeDatabaseEntity
    {
        public string Category { get; private set; }
        public string Name { get; private set; }

        public virtual void Deserialize(XmlNode node)
        {
            Category = node.SelectSingleNode(@"Property[@id='Category']")?.InnerText;

            Name = node.SelectSingleNode(@"Property[@id='Name']")?.InnerText;
        }

        protected bool Equals(PoeDatabaseEntity other)
        {
            return string.Equals(Category, other.Category) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            var other = obj as PoeDatabaseEntity;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Category != null ? Category.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
}