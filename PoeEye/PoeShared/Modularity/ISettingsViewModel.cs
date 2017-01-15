using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface ISettingsViewModel<TConfig> : ISettingsViewModel 
        where TConfig : class, IPoeEyeConfig, new()
    {
        void Load(TConfig config);

        TConfig Save();
    }

    public interface ISettingsViewModel
    {
        string ModuleName { [NotNull] get; }
    }
}