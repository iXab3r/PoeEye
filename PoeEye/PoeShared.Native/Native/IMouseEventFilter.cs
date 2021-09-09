using System.Windows.Forms;
using WindowsHook;

namespace PoeShared.Native
{
    public interface IMouseEventFilter
    {
        bool ShouldProcess(MouseEventExtArgs eventArgs);
    }
}