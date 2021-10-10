namespace WindowsHook
{
    public interface IKeyboardEventFilter
    {
        bool ShouldProcess(KeyEventArgsExt eventArgs);
    }
}