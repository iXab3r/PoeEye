using ReactiveUI;

namespace PoeShared.Scaffolding.WPF
{
    internal sealed class EditableTuple<T1, T2> : ReactiveObject
    {

        public T1 Item1 { get; set; }

        public T2 Item2 { get; set; }
    }
}