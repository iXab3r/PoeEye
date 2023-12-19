using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using PoeShared.Blazor;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Logging;
using PoeShared.Modularity;

namespace PoeShared.Tests.Meta;

public abstract class MetaFixtureBase : FixtureBase
{
    private new static readonly IFluentLog Log = typeof(MetaFixtureBase).PrepareLogger();

    public static IReadOnlyList<ModuleDefMD> AllModules { get; }

    static MetaFixtureBase()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        var context = new ModuleContext();
        AllModules = new[]
            {
                baseDir.GetFiles("*.dll", SearchOption.AllDirectories),
                baseDir.GetFiles("*.exe", SearchOption.AllDirectories),
            }
            .SelectMany(x => x)
            .Except(new[]{ new FileInfo(currentAssembly.Location) })
            .Select(x => LoadModuleDef(File.ReadAllBytes(x.FullName), context, x.FullName))
            .Where(x => x != null)
            .ToArray();

        Log.Debug($"Loaded {AllModules.Count} modules");
    }
    
    [Test]
    [TestCaseSource(nameof(EnumerateAllModules))]
    public void ShouldHaveAssemblyHasBlazorViews(ModuleDefMD module)
    {
        //Given
        var viewBaseType = typeof(BlazorReactiveComponent);

        //When
        var allViews =
            (from type in module.GetTypes()
                where type.IsClass && !type.IsAbstract && type.BaseType != null && string.Equals(type.BaseType.TypeName, viewBaseType.Name, StringComparison.Ordinal)
                select type).ToArray();
        Log.Debug($"Detected views:\n\t{allViews.DumpToTable()}");

        //Then
        foreach (var viewType in allViews)
        {
            var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasBlazorViewsAttribute).FullName);
            attribute.ShouldNotBeNull($"Assembly {module} contains view {viewType} thus it must has attribute {typeof(AssemblyHasBlazorViewsAttribute)} on it");
        }
    }


    [Test]
    [TestCaseSource(nameof(EnumerateAllModules))]
    public void ShouldHaveAssemblyHasPoeConfigConverters(ModuleDefMD module)
    {
        //Given
        var converterBaseType = typeof(ConfigMetadataConverter<,>);

        //When
        var allConverters =
            (from type in module.GetTypes()
                where type.IsClass && !type.IsAbstract && type.BaseType != null && string.Equals(type.BaseType.TypeName, converterBaseType.Name, StringComparison.Ordinal)
                select type).ToArray();
        Log.Debug($"Detected converters:\n\t{allConverters.DumpToTable()}");

        //Then
        foreach (var converter in allConverters)
        {
            var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasPoeConfigConvertersAttribute).FullName);
            attribute.ShouldNotBeNull($"Assembly {module} contains converter {converter} thus it must has attribute {typeof(AssemblyHasPoeConfigConvertersAttribute)} on it");
        }
    }

    [Test]
    [TestCaseSource(nameof(EnumerateAllModules))]
    public void ShouldHaveAssemblyHasPoeMetadataReplacementsAttribute(ModuleDefMD module)
    {
        //Given
        var baseType = typeof(IPoeConfigMetadataReplacementProvider);

        //When
        var allMetadataReplacements =
            (from type in module.GetTypes()
                where type.IsClass && !type.IsAbstract && type.Interfaces != null && type.Interfaces.Any(y => y.Interface.FullName == baseType.FullName)
                select type).ToArray();
        Log.Debug($"Detected metadata providers:\n\t{allMetadataReplacements.DumpToTable()}");

        //Then
        foreach (var metadataReplacement in allMetadataReplacements)
        {
            var assembly = metadataReplacement.Module.Assembly;
            var attribute = assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasPoeMetadataReplacementsAttribute).FullName);
            attribute.ShouldNotBeNull($"Assembly {assembly} contains metadata replacements {metadataReplacement} thus it must has attribute {typeof(AssemblyHasPoeMetadataReplacementsAttribute)} on it");
        }
    }

    public static IEnumerable<ModuleDefMD> EnumerateAllModules()
    {
        foreach (var module in AllModules)
        {
            yield return module;
        }
    }

    private static ModuleDefMD LoadModuleDef(byte[] assemblyBytes, ModuleContext moduleContext, string fileName = null)
    {
        try
        {
            return ModuleDefMD.Load(assemblyBytes, moduleContext);
        }
        catch (BadImageFormatException e)
        {
            Log.Warn($"Could not load DLL as .NET assembly - native or encrypted image ?, binary{(string.IsNullOrEmpty(fileName) ? "from memory" : "from file " + fileName)} size: {assemblyBytes.Length} - {e.Message}");
            return null;
        }
        catch (Exception e)
        {
            Log.Warn($"Exception occured when tried to parse DLL metadata, binary size: {assemblyBytes.Length}", e);
            return null;
        }
    }
}