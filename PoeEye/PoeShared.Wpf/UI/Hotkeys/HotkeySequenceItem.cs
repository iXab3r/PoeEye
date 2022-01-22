using System;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public abstract class HotkeySequenceItem : DisposableReactiveObject, ICloneable
{
    public object Clone()
    {
        return this.CloneJson();
    }
}