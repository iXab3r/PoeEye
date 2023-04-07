using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using PoeShared.Logging;
using PoeShared.Modularity;

namespace PoeShared.Tests.Meta;

public abstract class MetaFixtureBase : FixtureBase
{
    private new static readonly IFluentLog Log = typeof(MetaFixtureBase).PrepareLogger();

    public static IReadOnlyList<ModuleDefMD> AllModules { get; }

    static MetaFixtureBase()
    {
        var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        var context = new ModuleContext();
        AllModules = new[]
            {
                baseDir.GetFiles("*.dll"),
                baseDir.GetFiles("*.exe"),
            }
            .SelectMany(x => x)
            .Select(x => LoadModuleDef(File.ReadAllBytes(x.FullName), context, x.FullName))
            .Where(x => x != null)
            .ToArray();
        
        Log.Debug($"Loaded {AllModules.Count} modules");
    }
    
      [Test]
    public void ShouldHaveAssemblyHasPoeConfigConverters()
    {
        //Given
        var converterBaseType = typeof(ConfigMetadataConverter<,>);

        //When
        var allConverters =
            (from assembly in AllModules
                from type in assembly.GetTypes()
                where type.IsClass && !type.IsAbstract && type.BaseType != null && string.Equals(type.BaseType.TypeName, converterBaseType.Name, StringComparison.Ordinal)
                select type).ToArray();
        Log.Debug($"Detected converters:\n\t{allConverters.DumpToTable()}");

        //Then
        foreach (var converter in allConverters)
        {
            var module = converter.Module;
            var attribute = module.Assembly.CustomAttributes.FirstOrDefault(x => x.TypeFullName == typeof(AssemblyHasPoeConfigConvertersAttribute).FullName);
            attribute.ShouldNotBeNull($"Assembly {module} contains converter {converter} thus it must has attribute {typeof(AssemblyHasPoeConfigConvertersAttribute)} on it");
        }
    }

    [Test]
    public void ShouldHaveAssemblyHasPoeMetadataReplacementsAttribute()
    {
        //Given
        var baseType = typeof(IPoeConfigMetadataReplacementProvider);

        //When
        var allMetadataReplacements =
            (from assembly in AllModules
                from type in assembly.GetTypes()
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