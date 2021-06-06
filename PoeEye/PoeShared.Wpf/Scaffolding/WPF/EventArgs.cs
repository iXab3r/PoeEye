using System;

namespace PoeShared.Scaffolding.WPF
{
    public class EventArgs<T> : EventArgs
    {
        public T Value { get; set; }

        public EventArgs() { }
        
        public EventArgs(T value)
        {
            Value = value;
        }
    }
}
