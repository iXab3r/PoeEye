﻿using System;
using Gma.System.MouseKeyHook;

namespace PoeShared.Native
{
    internal interface IKeyboardMouseEventsProvider
    {
        IObservable<IKeyboardMouseEvents> System { get; }
        IObservable<IKeyboardMouseEvents> Application { get; }
    }
}