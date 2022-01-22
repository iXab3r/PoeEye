using System.Collections.ObjectModel;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Bindings;

public sealed class TestReactiveContainer : DisposableReactiveObject
{
    public ObservableCollection<TestObject> Containers { get; } = new ObservableCollection<TestObject>();
}