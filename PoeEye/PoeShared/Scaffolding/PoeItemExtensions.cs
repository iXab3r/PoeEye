using System;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.Scaffolding
{
    public static class PoeItemExtensions
    {
        public static string ToShortDescription([NotNull] this IPoeItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            return $"{item.ItemName} (tab: {item.TabName ?? "unknownTab"} @ {item.PositionInsideTab}) id: {item.Hash}";
        }
    }
}