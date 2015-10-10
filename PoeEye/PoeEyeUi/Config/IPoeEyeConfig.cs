namespace PoeEyeUi.Config
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    internal interface IPoeEyeConfig
    {
        PoeEyeTabConfig[] TabConfigs { [NotNull] get; [NotNull] set; }

        IDictionary<string, float> CurrenciesPriceInChaos { [NotNull] get; [NotNull] set; }
    }
}