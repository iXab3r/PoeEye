using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls.Services;

public static class JSComponentConfigurationExtensions
{
    public static void AddReactiveCollectionComponents(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterForJavaScript(typeof(ReactiveCollectionItemHost), ReactiveCollectionItemHost.ComponentIdentifier);
    }

    public static void AddDynamicComponents(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterForJavaScript(typeof(DynamicComponentContainer), "blazor-dynamic-component");
    }
}
