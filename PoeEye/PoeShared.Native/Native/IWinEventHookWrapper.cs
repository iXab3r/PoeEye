﻿using System;
using JetBrains.Annotations;

namespace PoeShared.Native;

public interface IWinEventHookWrapper
{
    [NotNull]
    IObservable<WinEventHookData> WhenWindowEventTriggered { get; }
}