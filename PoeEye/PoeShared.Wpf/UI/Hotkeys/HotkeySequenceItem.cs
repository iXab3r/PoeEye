using System;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI;

public abstract class HotkeySequenceItem : DisposableReactiveObject, ICloneable
{
    public object Clone()
    {
        return this.CloneJson();
    }
}