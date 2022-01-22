namespace WindowsHook;

public interface IMouseEventFilter
{
    bool ShouldProcess(MouseEventExtArgs eventArgs);
}