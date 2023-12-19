using System;
using System.Linq;
using dnlib.DotNet;
using PoeShared.Blazor;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Modularity;
using AssemblyHasPoeConfigConvertersAttribute = PoeShared.Modularity.AssemblyHasPoeConfigConvertersAttribute;

namespace PoeShared.Tests.Meta;

[TestFixture]
public class MetaFixture : MetaFixtureBase
{
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
    
}