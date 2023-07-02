using System;
using WindowsHook;

namespace PoeShared.Native;

internal interface IKeyboardMouseEventsProvider
{
    IObservable<IKeyboardMouseEvents> System { get; }
    
    IObservable<IKeyboardMouseEvents> Application { get; }
    
    bool SystemHookIsActive { get; }
    
    bool AppHookIsActive { get; }
}