using System.Globalization;
using PoeEye.ExileToolsApi.Entities;
using PoeShared.Common;
using TypeConverter;

namespace PoeEye.ExileToolsApi.Converters
{
    public class ToPoeItemConverter : IConverter<ExTzItem, IPoeItem>
    {
        public IPoeItem Convert(ExTzItem value)
        {
            if (value == null)
            {
                return null;
            }

            var result = new PoeItem()
            {
                ItemName = value.Info?.Name,
                CriticalChance = value.Properties?.Weapon?.CriticalStrikeChance.ToString(CultureInfo.InvariantCulture),
                AttacksPerSecond = value.Properties?.Weapon?.AttacksPerSecond.ToString(CultureInfo.InvariantCulture),
                DamagePerSecond = value.Properties?.Weapon?.TotalDps.ToString(CultureInfo.InvariantCulture),
                PhysicalDamagePerSecond = value.Properties?.Weapon?.PhysicalDps.ToString(CultureInfo.InvariantCulture),
                Level = value.Attributes?.ItemLevel.ToString(CultureInfo.InvariantCulture),
                IsCorrupted = value.Attributes?.IsCorrupted != null && (bool) value.Attributes?.IsCorrupted,
            };

            return result;
        }

    }
}