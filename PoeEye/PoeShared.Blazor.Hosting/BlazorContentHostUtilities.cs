using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PoeShared.Scaffolding;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace PoeShared.Blazor.Wpf;

internal static class BlazorContentHostUtilities
{
    public static IServiceProvider BuildUnityServiceProvider(IUnityContainer container, IServiceCollection serviceCollection)
    {
        var factory = new ServiceProviderFactory(container);
        return factory.CreateServiceProvider(serviceCollection);
    }

    public static string FormatExceptionMessage(Exception exception)
    {
        return exception.ToString();
    }

    public static string PrepareIndexFileContext(string template, IReadOnlyList<IFileInfo> additionalFiles)
    {
        var cssLinksText = additionalFiles
            .Where(x => x.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.css", StringComparison.OrdinalIgnoreCase))
            .Select(file => $"<link href=\"{file.Name}\" rel=\"stylesheet\"></link>")
            .JoinStrings(Environment.NewLine);

        var scriptsText = additionalFiles
            .Where(x => x.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.js", StringComparison.OrdinalIgnoreCase))
            .Select(file => $"<script src=\"{file.Name}\"></script>")
            .JoinStrings(Environment.NewLine);

        return template
            .Replace("<!--% AdditionalStylesheetsBlock %-->", cssLinksText)
            .Replace("<!--% AdditionalScriptsBlock %-->", scriptsText);
    }
}
