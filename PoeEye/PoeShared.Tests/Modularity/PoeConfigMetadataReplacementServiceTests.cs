using System.Linq;
using AutoFixture;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Modularity;

[TestFixture]
public class PoeConfigMetadataReplacementServiceTests : FixtureBase
{
    [Test]
    public void ShouldRegisterReplacement()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.AddMetadataReplacement("test", typeof(int));

        //Then
        instance.Replacements.Count.ShouldBe(1);
        instance.Replacements[0].SourceTypeName.ShouldBe("test");
        instance.Replacements[0].TargetMetadata.TypeName.ShouldBe(typeof(int).FullName);
        instance.Replacements[0].TargetMetadata.AssemblyName.ShouldBe(typeof(int).Assembly.GetName().Name);
    }
    
    [Test]
    public void ShouldRegisterMultipleReplacementsForTheDifferentTypes()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.AddMetadataReplacement("test1", typeof(int));
        instance.AddMetadataReplacement("test2", typeof(short));

        //Then
        instance.Replacements.Count.ShouldBe(2);

        var replacement1 = instance.Replacements.First(x => x.SourceTypeName == "test1");
        replacement1.TargetMetadata.TypeName.ShouldBe(typeof(int).FullName);
        replacement1.TargetMetadata.AssemblyName.ShouldBe(typeof(int).Assembly.GetName().Name);
        
        var replacement2 = instance.Replacements.First(x => x.SourceTypeName == "test2");
        replacement2.TargetMetadata.TypeName.ShouldBe(typeof(short).FullName);
        replacement2.TargetMetadata.AssemblyName.ShouldBe(typeof(short).Assembly.GetName().Name);
    }
    
    [Test]
    public void ShouldRegisterMultipleReplacementsForTheSameType()
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.AddMetadataReplacement("test1", typeof(int));
        instance.AddMetadataReplacement("test2", typeof(int));

        //Then
        instance.Replacements.Count.ShouldBe(2);

        var replacement1 = instance.Replacements.First(x => x.SourceTypeName == "test1");
        replacement1.TargetMetadata.TypeName.ShouldBe(typeof(int).FullName);
        replacement1.TargetMetadata.AssemblyName.ShouldBe(typeof(int).Assembly.GetName().Name);
        
        var replacement2 = instance.Replacements.First(x => x.SourceTypeName == "test2");
        replacement2.TargetMetadata.TypeName.ShouldBe(typeof(int).FullName);
        replacement2.TargetMetadata.AssemblyName.ShouldBe(typeof(int).Assembly.GetName().Name);
    }

    [Test]
    public void ShouldNotifyWhenMetadataWasReplaced()
    {
        //Given
        var instance = CreateInstance();

        var sourceMetadata = new PoeConfigMetadata() {TypeName = "test"};
        var updates = instance.Watch(sourceMetadata).Listen();
        updates.Single().ShouldBeSameAs(sourceMetadata);
        
        //When
        var replacement = instance.AddMetadataReplacement("test", typeof(int));

        //Then
        updates[1].ShouldBeSameAs(replacement);
        replacement.TypeName.ShouldBe(typeof(int).FullName);
        replacement.AssemblyName.ShouldBe(typeof(int).Assembly.GetName().Name);
    }

    [Test]
    public void ShouldNotifyWhenAddedReplacementForType()
    {
        //Given
        var instance = CreateInstance();
        
        var updates = instance.WatchForAddedReplacements(typeof(int)).Listen();
        updates[0].ShouldBe(typeof(int).FullName);

        //When
        instance.AddMetadataReplacement("test", typeof(int));

        //Then
        updates[1].ShouldBe("test");
    }
    
    private PoeConfigMetadataReplacementService CreateInstance()
    {
        return Container.Create<PoeConfigMetadataReplacementService>();
    }
}