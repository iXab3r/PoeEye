using System.Reflection;
using System.Resources;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

/// <summary>
/// Provides extension methods for <see cref="Assembly"/> and <see cref="AssemblyName"/>.
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Determines whether the assembly is likely code-generated (e.g., by Roslyn, REPL, or other tools).
    /// </summary>
    /// <param name="assemblyName">The assembly name to check.</param>
    /// <returns><c>true</c> if the assembly is code-generated; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This check is based on common naming conventions for dynamic/generated assemblies, 
    /// such as those starting with "ℛ*" or "ℛ_" (REPL-specific) or "Microsoft.GeneratedCod".
    /// </remarks>
    public static bool IsCodeGenerated(this AssemblyName assemblyName)
    {
        return assemblyName.Name.StartsWith("ℛ*") || //legacy REPL assemblies
               assemblyName.Name.StartsWith("ℛ_") ||
               assemblyName.Name.StartsWith("Microsoft.GeneratedCod");
    }

    /// <summary>
    /// Determines whether the assembly is likely code-generated.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns><c>true</c> if the assembly is code-generated; otherwise, <c>false</c>.</returns>
    public static bool IsCodeGenerated(this Assembly assembly)
    {
        var assemblyName = assembly.GetName();
        return assemblyName.IsCodeGenerated();
    }

    /// <summary>
    /// Reads an embedded manifest resource from the specified assembly as a string.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="path">The relative path to the resource (without the assembly name prefix).</param>
    /// <returns>The content of the resource as a string.</returns>
    /// <exception cref="MissingManifestResourceException">Thrown if the resource cannot be found.</exception>
    /// <remarks>
    /// The method assumes the resource path in the manifest starts with the assembly name.
    /// For example, if the assembly is "MyApp" and the path is "Resources.Config.xml", 
    /// it looks for "MyApp.Resources.Config.xml".
    /// </remarks>
    public static string ReadResourceAsString(this Assembly assembly, [NotNull] string path)
    {
        Guard.ArgumentNotNull(path, nameof(path));

        var assemblyName = assembly.GetName().Name;
        var resourcePath = $"{assemblyName}.{path}";
        using (var stream = assembly.GetManifestResourceStream(resourcePath))
        {
            if (stream == null)
            {
                var assemblyResources = assembly.GetManifestResourceNames();
                throw new MissingManifestResourceException(
                    $"Could not find resource '{resourcePath}'. Assembly '{assemblyName}' resources list:\n\t{assemblyResources.DumpToString()}");
            }

            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}