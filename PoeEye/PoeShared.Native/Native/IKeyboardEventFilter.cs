using System.Windows.Forms;
using WindowsHook;

namespace PoeShared.Native
{
    public interface IKeyboardEventFilter
    {
        bool ShouldProcess(KeyEventArgsExt eventArgs);
    }
}