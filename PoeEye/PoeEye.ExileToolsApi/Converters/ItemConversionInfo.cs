using Guards;
using PoeEye.ExileToolsApi.Entities;

namespace PoeEye.ExileToolsApi.Converters
{
    internal sealed class ItemConversionInfo
    {
        public ExTzItem Item { get; private set; }

        public string[] AdditionalModsToInclude { get; private set; }

        public ItemConversionInfo(ExTzItem item) : this(item, new string[0]) { }

        public ItemConversionInfo(ExTzItem item, string[] additionalModsToInclude)
        {
            Guard.ArgumentNotNull(() => item);
            Guard.ArgumentNotNull(() => additionalModsToInclude);

            Item = item;
            AdditionalModsToInclude = additionalModsToInclude;
        }
    }
}