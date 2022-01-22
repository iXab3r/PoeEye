using System.Windows.Forms;
using System.Windows.Input;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI;

internal sealed class KeysToModifierKeysConverter : IConverter<Keys, ModifierKeys>
{
    public ModifierKeys Convert(Keys value)
    {
        return value.ToModifiers();
    }
}