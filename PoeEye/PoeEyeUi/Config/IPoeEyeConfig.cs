namespace PoeEyeUi.Config
{
    using JetBrains.Annotations;

    internal interface IPoeEyeConfig
    {
        PoeEyeTabConfig[] TabConfigs { [NotNull] get; [NotNull] set; }
    }
}