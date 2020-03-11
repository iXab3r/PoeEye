namespace PoeShared.Native
{
    internal sealed class AppArgumentsForWindows : AppArguments
    {
        public AppArgumentsForWindows()
        {
            IsElevated = UnsafeNative.IsElevated();
        }
    }
}