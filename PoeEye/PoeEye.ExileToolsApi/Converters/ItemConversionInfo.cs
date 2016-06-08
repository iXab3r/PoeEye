using Guards;
using Newtonsoft.Json.Linq;
using PoeEye.ExileToolsApi.Entities;

namespace PoeEye.ExileToolsApi.Converters
{
    internal sealed class ItemConversionInfo
    {
        public JRaw Item { get; private set; }

        public string[] AdditionalModsToInclude { get; private set; }

        public ItemConversionInfo(JRaw item) : this(item, new string[0]) { }

        public ItemConversionInfo(JRaw item, string[] additionalModsToInclude)
        {
            Guard.ArgumentNotNull(() => item);
            Guard.ArgumentNotNull(() => additionalModsToInclude);

            Item = item;
            AdditionalModsToInclude = additionalModsToInclude;
        }
    }
}