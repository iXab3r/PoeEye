using DynamicData;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Modularity;

internal sealed class PoeEyeModulesRegistrator : IPoeEyeModulesRegistrator, IPoeEyeModulesEnumerator
{
    private readonly IUnityContainer container;

    private readonly SourceListEx<ISettingsViewModel> settings = new();

    public PoeEyeModulesRegistrator(IUnityContainer container)
    {
        Guard.ArgumentNotNull(container, nameof(container));

        this.container = container;
        Settings = settings;
    }

    public IObservableList<ISettingsViewModel> Settings { get; }

    public IPoeEyeModulesRegistrator RegisterSettingsEditor<TConfig, TSettingsViewModel>()
        where TConfig : class, IPoeEyeConfig, new()
        where TSettingsViewModel : ISettingsViewModel<TConfig>
    {
        var viewModel = (ISettingsViewModel) container.Resolve(typeof(TSettingsViewModel));
        settings.Add(viewModel);

        return this;
    }
}