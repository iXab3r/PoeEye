using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Scaffolding;

namespace PoeShared.Blazor.Controls.Services;

public static class JSComponentConfigurationExtensions
{
    public static void AddReactiveCollectionComponents(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterForJavaScriptIfMissing(
            typeof(ReactiveCollectionItemHost),
            ReactiveCollectionItemHost.ComponentIdentifier);
    }

    public static void AddDynamicComponents(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterForJavaScriptIfMissing(
            typeof(DynamicComponentContainer),
            DynamicComponentContainer.ComponentIdentifier);
    }
}
