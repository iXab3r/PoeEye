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
        var baseTypes = new[] { typeof(BlazorReactiveComponent<>) };

        //When
        var allViews = FindImplementations(module, baseTypes).ToList();
        Log.Debug($"Detected views:\n\t{allViews.DumpToTable()}");

        //Then
        var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasBlazorViewsAttribute).FullName);
        if (allViews.Any())
        {
            attribute.ShouldNotBeNull($"Assembly {module} contains Blazor views thus it must have attribute {typeof(AssemblyHasBlazorViewsAttribute)} on it:\n\t{allViews.Select(x => x.FullName).DumpToTable()}");
        }
        else
        {
            attribute.ShouldBeNull($"Assembly {module} does not contain any blazor views thus it does not need attribute {typeof(AssemblyHasBlazorViewsAttribute)} on it");
        }
    }


    [Test]
    [TestCaseSource(nameof(EnumerateAllModules))]
    public void ShouldHaveAssemblyHasPoeConfigConverters(ModuleDefMD module)
    {
        //Given
        var converterBaseType = typeof(ConfigMetadataConverter<,>);

        //When
        var allConverters = FindImplementations(module, converterBaseType).ToList();
        Log.Debug($"Detected converters:\n\t{allConverters.DumpToTable()}");

        //Then
        var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasPoeConfigConvertersAttribute).FullName);
        if (allConverters.Any())
        {
            attribute.ShouldNotBeNull($"Assembly {module} contains converters thus it must have attribute {typeof(AssemblyHasPoeConfigConvertersAttribute)} on it:\n\t{allConverters.Select(x => x.FullName).DumpToTable()}");
        }
        else
        {
            attribute.ShouldBeNull($"Assembly {module} does not contain any converters thus it does not need attribute {typeof(AssemblyHasPoeConfigConvertersAttribute)} on it");
        }
    }

    [Test]
    [TestCaseSource(nameof(EnumerateAllModules))]
    public void ShouldHaveAssemblyHasPoeMetadataReplacementsAttribute(ModuleDefMD module)
    {
        //Given
        var baseType = typeof(IPoeConfigMetadataReplacementProvider);

        //When
        var allMetadataReplacements = FindImplementations(module, baseType).ToList();
        Log.Debug($"Detected metadata providers:\n\t{allMetadataReplacements.DumpToTable()}");

        //Then
        var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasPoeMetadataReplacementsAttribute).FullName);
        if (allMetadataReplacements.Any())
        {
            attribute.ShouldNotBeNull($"Assembly {module} contains metadata replacements thus it must have attribute {typeof(AssemblyHasPoeMetadataReplacementsAttribute)} on it:\n\t{allMetadataReplacements.Select(x => x.FullName).DumpToTable()}");
        }
        else
        {
            attribute.ShouldBeNull($"Assembly {module} does not contain any metadata replacements thus it does not need attribute {typeof(AssemblyHasPoeMetadataReplacementsAttribute)} on it");
        }
    }

    protected static IEnumerable<TypeDef> FindImplementations(ModuleDefMD module, params Type[] baseTypes)
    {
        var allTypes =
            (from type in module.GetTypes()
                let isMatch = type.IsClass && !type.IsAbstract && type.BaseType != null &&  baseTypes.Any(baseType => IsMatch(type, baseType)) 
                select new { type, isMatch}).ToList();
        Log.Debug($"Analysis of module {module}, base types:\n\t{baseTypes.Select(x => $"{x.FullName} (isInterface: {x.IsInterface})").DumpToTable()}");
        foreach (var typeInfo in allTypes)
        {
            Log.Debug($"IsMatch: {typeInfo.isMatch}, Type: {typeInfo.type}");
        }
        return allTypes.Where(x => x.isMatch).Select(x => x.type).ToList();
    }

    protected static bool IsMatch(TypeDef typeDef, Type baseType)
    {
        if (baseType.IsInterface)
        {
            return typeDef.Interfaces.Any(x => x.Interface.FullName.StartsWith(baseType.FullName!, StringComparison.Ordinal));
        }

        return typeDef.BaseType.FullName.StartsWith(baseType.FullName!, StringComparison.Ordinal);
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