using JetBrains.Annotations;

namespace PoeShared.Prism
{
    public interface IFactory<out TOut>
    {
        [NotNull]
        TOut Create();
    }

    public interface IFactory<out TOut, in TIn1>
    {
        [NotNull]
        TOut Create(TIn1 param1);
    }

    public interface IFactory<out TOut, in TIn1, in TIn2>
    {
        [NotNull]
        TOut Create(TIn1 param1, TIn2 param2);
    }
    
    public interface IFactory<out TOut, in TIn1, in TIn2, in TIn3>
    {
        [NotNull]
        TOut Create(TIn1 param1, TIn2 param2, TIn3 param3);
    }
}