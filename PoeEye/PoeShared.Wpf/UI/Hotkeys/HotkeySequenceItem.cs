using System;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public abstract class HotkeySequenceItem : ICloneable
    {
        public object Clone()
        {
            return this.CloneJson();
        }
    }
}