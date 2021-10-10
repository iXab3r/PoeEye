using System;

namespace WindowsHook
{
    internal sealed class PassthroughKeyboardEventFilter : IKeyboardEventFilter
    {
        private static readonly Lazy<IKeyboardEventFilter> InstanceSupplier = new(() => new PassthroughKeyboardEventFilter());

        public static IKeyboardEventFilter Instance => InstanceSupplier.Value;
        
        public bool ShouldProcess(KeyEventArgsExt eventArgs)
        {
            return true;
        }
    }
}