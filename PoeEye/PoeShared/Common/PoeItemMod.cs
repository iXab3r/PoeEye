using System.Collections.Generic;
using PoeShared.Scaffolding;

namespace PoeShared.Common
{
    public sealed class PoeItemMod : IPoeItemMod
    {
        public static IEqualityComparer<PoeItemMod> CodeNameComparer { get; } = new LambdaComparer<PoeItemMod>((x, y) => string.Equals(x.CodeName, y.CodeName));
        public static IEqualityComparer<PoeItemMod> NameComparer { get; } = new LambdaComparer<PoeItemMod>((x, y) => string.Equals(x.Name, y.Name));

        public string TierInfo { get; set; }

        public PoeModType ModType { get; set; }

        public PoeModOrigin Origin { get; set; }

        public string Name { get; set; }

        public string CodeName { get; set; }

        public override string ToString()
        {
            return $"ModType: {ModType}, CodeName: {CodeName}";
        }
    }
}