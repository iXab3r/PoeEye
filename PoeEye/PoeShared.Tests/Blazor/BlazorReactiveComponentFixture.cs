using System;
using System.Collections.Generic;
using DynamicData;
using PoeShared.Blazor;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Blazor;

[TestFixture]
internal class BlazorReactiveComponentFixtureTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    [TestCaseSource(nameof(ShouldRefreshTestCases))]
    public void ShouldRefresh(Action<TestComponent> setter, int expectedChanges)
    {
        //Given
        var instance = CreateInstance();
        var context = new TestViewModel()
        {
            Nested = new TestViewModel()
        };
        instance.DataContext = context;
        var changes = instance.WhenChanged.Listen();

        //When
        setter(instance);

        //Then
        changes.Count.ShouldBe(expectedChanges);
    }

    private TestComponent CreateInstance()
    {
        return new TestComponent();
    }

    public sealed class TestComponent : BlazorReactiveComponent<TestViewModel>
    {
    }

    public static IEnumerable<NamedTestCaseData> ShouldRefreshTestCases()
    {
        yield return AsCase("Struct property", x => x.DataContext.IntValue = 1, expectedChanges: 1);
        yield return AsCase("ObservableCollection Add", x => x.DataContext.ObservableCollection.Add(1), expectedChanges: 1);
        yield return AsCase("SourceList Add", x => x.DataContext.SourceList.Add(1), expectedChanges: 1);
        yield return AsCase("ObservableList Add", x => x.DataContext.AddToObservableList(1), expectedChanges: 1);
        yield return AsCase("ObservableCache Add", x => x.DataContext.AddToObservableCache(1), expectedChanges: 1);
        yield return AsCase("SourceCache Add", x => x.DataContext.SourceCache.AddOrUpdate(1), expectedChanges: 1);
        yield return AsCase("UntypedObservableCollection Add", x => x.DataContext.AddToUntypedObservableCollection(1), expectedChanges: 1);
        yield return AsCase("UntypedObservableList Add", x => x.DataContext.AddToUntypedObservableList(1), expectedChanges: 1);
        yield return AsCase("UntypedObservableCache Add", x => x.DataContext.AddToUntypedObservableCache(1), expectedChanges: 1);

        //FIXME Not supported yet, but should be!
        yield return AsCase("Nested Struct property", x => x.DataContext.Nested.IntValue = 1, expectedChanges: 0);
    }

    private static NamedTestCaseData AsCase(string testName, Action<TestComponent> setter, int expectedChanges)
    {
        return new NamedTestCaseData(setter, expectedChanges)
        {
            TestName = testName
        };
    }

    public sealed class TestViewModel : DisposableReactiveObjectWithLogger
    {
        private readonly SourceList<int> observableList = new();
        private readonly SourceCache<int, int> observableCache = new(x => x);
        private readonly ObservableCollectionEx<int> untypedObservableCollection = new();
        private readonly SourceList<int> untypedObservableList = new();
        private readonly SourceCache<int, int> untypedObservableCache = new(x => x);
        
        
        
        public int IntValue { get; set; }

        public ObservableCollectionEx<int> ObservableCollection { get; } = new();

        public object UntypedObservableList => untypedObservableList;
        
        public object UntypedObservableCollection => untypedObservableCollection;
        
        public object UntypedObservableCache => untypedObservableCache;

        public SourceListEx<int> SourceList { get; } = new();

        /// <summary>
        /// IObservableList does not implement INotifyPropertyChanged, that is why we need another test case
        /// </summary>
        public IObservableList<int> ObservableList => observableList;

        public SourceCache<int, int> SourceCache { get; } = new(x => x);
        
        public IObservableCache<int, int> ObservableCache => observableCache;

        public TestViewModel Nested { get; init; }

        public void AddToObservableList(int value)
        {
            observableList.Add(value);
        }
        
        public void AddToUntypedObservableCollection(int value)
        {
            untypedObservableCollection.Add(value);
        }
        
        public void AddToUntypedObservableList(int value)
        {
            untypedObservableList.Add(value);
        }
        
        public void AddToUntypedObservableCache(int value)
        {
            untypedObservableCache.AddOrUpdate(value);
        }

        public void AddToObservableCache(int value)
        {
            observableCache.AddOrUpdate(value);
        }

        public TestViewModel()
        {
        }
    }
}