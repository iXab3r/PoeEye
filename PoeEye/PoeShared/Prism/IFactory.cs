﻿using JetBrains.Annotations;

namespace PoeShared.Prism;

public interface IFactory<out TOut>
{
    [NotNull]
    TOut Create();
}

public interface IScopedFactory<out TOut> : IFactory<TOut>
{
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

public interface IFactory<out TOut, in TIn1, in TIn2, in TIn3, in TIn4>
{
    [NotNull]
    TOut Create(TIn1 param1, TIn2 param2, TIn3 param3, TIn4 param4);
}

public interface IFactory<out TOut, in TIn1, in TIn2, in TIn3, in TIn4, in TIn5>
{
    [NotNull]
    TOut Create(TIn1 param1, TIn2 param2, TIn3 param3, TIn4 param4, TIn5 param5);
}