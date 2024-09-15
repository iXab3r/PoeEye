using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity;

public class JsonConfigSerializerTests : FixtureBase
{
    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(32)]
    [TestCase(64)]
    [TestCase(128)]
    [TestCase(256)]
    [TestCase(512)] 
    //[TestCase(1024)] //does not seem to work reliably
    public async Task ShouldDeserializeDeeplyNestedObject(int depth)
    {
        await RunCodeWithDifferentStackSize(() => ShouldDeserializeDeeplyNestedObjectTest(depth), maxStackSize: 10 * 1024 * 1024);
    }

    private void ShouldDeserializeDeeplyNestedObjectTest(int depth)
    {
        //Given
        var testClass = new Nested { Id = 0 };

        var current = testClass;
        for (var idx = 0; idx < depth; idx++)
        {
            var child = new Nested() { Id = idx };
            current.Child = child;
            current = child;
        }

        var instance = CreateInstance();

        Log.Info($"Serializing:\n{testClass}");

        var json = instance.Serialize(testClass);
        
        Log.Info($"Serialized JSON:\n{json}");

        //When
        var deserialized = instance.Deserialize<Nested>(json);

        //Then
        Log.Info($"Deserialized:\n{deserialized}");
    }

    private async Task RunCodeWithDifferentStackSize(Action method, int maxStackSize)
    {
        var tcs = new TaskCompletionSource();
        
        Log.Info($"Creating new thread with stack size of {ByteSizeLib.ByteSize.FromBytes(maxStackSize)}");
        var thread = new Thread(() =>
        {
            try
            {
                Log.Info("Invoking the method");
                method();
                Log.Info("Invocation completed");
                tcs.SetResult();
            }
            catch (Exception e)
            {
                Log.Warn("Exception in the method", e);
                tcs.SetException(e);
            }
                
        }, maxStackSize);
        Log.Info($"Started the thread {thread}");
        thread.Start();

        Log.Info($"Awaiting for thread completion");
        await tcs.Task;
    }

    private JsonConfigSerializer CreateInstance()
    {
        return new JsonConfigSerializer();
    }
    
    private sealed record Nested
    {
        public long Id { get; set; }
        public Nested Child { get; set; }
    }
}