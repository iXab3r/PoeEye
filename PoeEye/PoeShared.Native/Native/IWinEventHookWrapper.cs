using System;
using JetBrains.Annotations;
using PInvoke;

namespace PoeShared.Native;

public interface IWinEventHookWrapper
{
    [NotNull]
    IObservable<WinEventHookData> WhenWindowEventTriggered { get; }
}