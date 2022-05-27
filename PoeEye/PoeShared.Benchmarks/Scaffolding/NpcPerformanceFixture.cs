using System.Collections.Concurrent;
using System.ComponentModel;
using BenchmarkDotNet.Diagnosers;
using PoeShared.Tests.Helpers;

namespace PoeShared.Benchmarks.Scaffolding;

[TestFixture]
[MemoryDiagnoser]
public class NpcPerformanceFixture : BenchmarkBase
{
    [Test]
    public void RunCollectionsBenchmarks()
    {
        BenchmarkRunner.Run<NpcPerformanceFixture>(Config);
    }

    protected override ManualConfig PrepareConfig()
    {
        var result = base.PrepareConfig();

        return result.AddDiagnoser(MemoryDiagnoser.Default);
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetModels))]
    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldCreate(IFake item)
    {
        //Given
        //When
        //Then
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetModels))]
    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldProcessSingularAssignmentWithoutSubscriber(IFake item)
    {
        //Given
        //When
        var itemsToAdd = Enumerable.Range(0, 100000).ToArray();
        itemsToAdd.ForEach(x => item.Value = x);

        //Then
    }
    
    [Benchmark]
    [ArgumentsSource(nameof(GetModels))]
    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldProcessSingularAssignmentWithSubscriber(IFake item)
    {
        //Given
        var items = new ConcurrentQueue<int>();
        item.Value = 0;
        item.WhenAnyValue(x => x.Value).Subscribe(items.Enqueue);
        
        //When
        var itemsToAdd = Enumerable.Range(0, 100000).ToArray();
        itemsToAdd.ForEach(x => item.Value = x);

        //Then
        items.CollectionSequenceShouldBe(itemsToAdd);
    }
    
    public static IEnumerable<IFake> GetModels()
    {
        yield return new FakeViaDefaultNpc();
        yield return new FakeViaReactiveObject();
        yield return new FakeViaThreadSafeNpc();
    }
    
    public interface IFake : INotifyPropertyChanged
    {
        int Value { get; set; }
    }

    private sealed class FakeViaReactiveObject : DisposableReactiveObject, IFake
    {
        public int Value { get; set; }
    }
    
    private sealed class FakeViaDefaultNpc : IFake
    {
        public int Value { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    private sealed class FakeViaThreadSafeNpc : IFake
    {
        private readonly ConcurrentNpcEventInvoker propertyChanged;

        public FakeViaThreadSafeNpc()
        {
            propertyChanged = new ConcurrentNpcEventInvoker(this);
        }

        public int Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => this.propertyChanged.Add(value);
            remove => this.propertyChanged.Remove(value);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (propertyChanged.IsEmpty)
            {
                return;
            }
            propertyChanged.Raise(propertyName);
        }
    }
}