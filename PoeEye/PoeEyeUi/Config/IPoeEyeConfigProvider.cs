using JetBrains.Annotations;

namespace PoeEyeUi.Config
{
    internal interface IPoeEyeConfigProvider<TConfig> where TConfig : IPoeEyeConfig
    {
        [NotNull] 
        TConfig Load();

        void Save([NotNull] TConfig config);
    }
}