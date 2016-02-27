namespace PoeShared.Common
{
    using System.Collections.Generic;

    public sealed class PoeItemMod : IPoeItemMod
    {
        public static IEqualityComparer<PoeItemMod> CodeNameComparer { get; } = new CodeNameEqualityComparer();

        public bool IsCrafted { get; set; }

        public PoeModType ModType { get; set; }

        public string Name { get; set; }

        public string CodeName { get; set; }

        public override string ToString()
        {
            return $"ModType: {ModType}, CodeName: {CodeName}, IsCrafted: {IsCrafted}";
        }

        private sealed class CodeNameEqualityComparer : IEqualityComparer<PoeItemMod>
        {
            public bool Equals(PoeItemMod x, PoeItemMod y)
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
                return string.Equals(x.CodeName, y.CodeName);
            }

            public int GetHashCode(PoeItemMod obj)
            {
                return obj.CodeName?.GetHashCode() ?? 0;
            }
        }
    }
}