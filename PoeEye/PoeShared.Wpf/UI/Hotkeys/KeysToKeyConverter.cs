using System.Windows.Forms;
using System.Windows.Input;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI
{
    internal sealed class KeysToKeyConverter : IConverter<Keys, Key>
    {
        public Key Convert(Keys value)
        {
            return value.ToInputKey();
        }
    }
}