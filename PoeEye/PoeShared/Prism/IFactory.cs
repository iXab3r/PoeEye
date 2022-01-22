using JetBrains.Annotations;

namespace PoeShared.Prism;

public interface IFactory<out TOut>
{
    [NotNull]
    TOut Create();
}

public interface INamedFactory<out TOut>
{
    [NotNull]
    TOut Create(string name);
}

public interface INamedFactory<out TOut, in TIn1>
{
    [NotNull]
    TOut Create(string name, TIn1 param1);
}
    
public interface INamedFactory<out TOut, in TIn1, in TIn2>
{
    [NotNull]
    TOut Create(string name, TIn1 param1, TIn2 param2);
}
    
public interface INamedFactory<out TOut, in TIn1, in TIn2, in TIn3>
{
    [NotNull]
    TOut Create(string name, TIn1 param1, TIn2 param2, TIn3 param3);
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