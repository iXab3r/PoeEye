using PropertyChanged;

namespace PoeShared.Benchmarks.PropertyBinder;

public class BinderPerformanceCollectionsFixture : BenchmarkBase
{
    [Test]
    public void RunCollectionsBenchmarks()
    {
        BenchmarkRunner.Run<BinderPerformanceCollectionsFixture>(Config);
    }
        
    [ParamsSource(nameof(GetModels))]
    public TestCollectionBase ViewModel { get; set; }
        
    public static IEnumerable<TestCollectionBase> GetModels() // for multiple arguments it's an IEnumerable of array of objects (object[])
    {
        yield return new ReactiveTestCollection();
        yield return new PropertyHelperTestCollection();
        yield return new BinderTestCollection();
    }
        
    [Benchmark(Description = "Collection Count")]
    [Arguments(100)]
    public void BinderModelCount(int count)
    {
        ViewModel.Collection.Clear();
        ViewModel.Collection.Count.ShouldBe(0);
        ViewModel.Count.ShouldBe(0);
        for (int i = 0; i < count; i++)
        {
            ViewModel.Collection.Add(i);
            ViewModel.Collection.Count.ShouldBe(i + 1);
            ViewModel.Count.ShouldBe(ViewModel.Collection.Count);
        }
    }
        
    [Benchmark(Description = "Collection Sum")]
    [Arguments(100)]
    public void BinderModelSum(int count)
    {
        ViewModel.Collection.Clear();
        ViewModel.Sum.ShouldBe(0);
        var sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += i;
            ViewModel.Collection.Add(i);
            ViewModel.Sum.ShouldBe(sum);
        }
    }

    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldSum(TestCollectionBase instance)
    {
        //Given
        instance.Sum.ShouldBe(0);
        instance.Collection.Add(1);

        //When
        instance.Sum.ShouldBe(1);
        instance.Collection.Add(2);

        //Then
        instance.Sum.ShouldBe(3);
    }
        
    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldSumOnItemChange(TestCollectionBase instance)
    {
        //Given
        instance.Sum.ShouldBe(0);
        instance.Collection.Add(1);

        //When
        instance.Sum.ShouldBe(1);
        instance.Collection.Replace(1, 2);

        //Then
        instance.Sum.ShouldBe(2);
    }
        
    [Test]
    [TestCaseSource(nameof(GetModels))]
    public void ShouldCount(TestCollectionBase instance)
    {
        //Given
        instance.Count.ShouldBe(0);
        instance.Collection.Add(1);

        //When
        instance.Count.ShouldBe(1);
        instance.Collection.Add(2);

        //Then
        instance.Count.ShouldBe(2);
    }
        
    [SuppressPropertyChangedWarnings]
    public abstract class TestCollectionBase : DisposableReactiveObject
    {
        public ObservableCollection<int> Collection { get; } = new();

        public abstract int Count { get; protected set; }
            
        public abstract int Sum { get; protected set; }
    }

    public sealed class PropertyHelperTestCollection : TestCollectionBase
    {
        private readonly ObservableAsPropertyHelper<int> count;
        private readonly ObservableAsPropertyHelper<int> sum;

        public PropertyHelperTestCollection()
        {
            this.Collection.ToObservableChangeSet()
                .CountIf()
                .ToProperty(out count, this, x => x.Count)
                .AddTo(Anchors);
                
            this.Collection.ToObservableChangeSet()
                .Sum(x => x)
                .ToProperty(out sum, this, x => x.Sum)
                .AddTo(Anchors);
        }

        public override int Sum
        {
            get => sum.Value;
            protected set => throw new System.NotImplementedException();
        }

        public override int Count
        {
            get => count.Value;
            protected set => throw new System.NotImplementedException();
        }
    }
        
    public sealed class BinderTestCollection : TestCollectionBase
    {
        private static readonly Binder<BinderTestCollection> Binder = new();

        static BinderTestCollection()
        {
            Binder.Bind(x => x.Collection.Sum()).To(x => x.Sum);
            Binder.Bind(x => x.Collection.Count()).To(x => x.Count);
        }

        public BinderTestCollection()
        {
            Binder.Attach(this).AddTo(Anchors);
        }

        public override int Sum { get; protected set; }

        public override int Count { get; protected set; }
    }
        
    public sealed class ReactiveTestCollection : TestCollectionBase
    {

        public ReactiveTestCollection()
        {
            this.Collection.ToObservableChangeSet()
                .CountIf()
                .Subscribe(x => Count = x)
                .AddTo(Anchors);
                
            this.Collection.ToObservableChangeSet()
                .Sum(x => x)
                .Subscribe(x => Sum = x)
                .AddTo(Anchors);
        }

        public override int Sum { get; protected set; }

        public override int Count { get; protected set; }
    }
}