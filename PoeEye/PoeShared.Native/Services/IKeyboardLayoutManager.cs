using System.Collections.ObjectModel;
using System.Globalization;

namespace PoeShared.Services
{
    public interface IKeyboardLayoutManager
    {
        KeyboardLayout GetCurrent();

        /// <summary>
        ///   LayoutId (NOT NAME), e.g. Russian = 00000419, English = 00000409
        /// </summary>
        /// <param name="keyboardLayoutName"></param>
        /// <returns></returns>
        KeyboardLayout ResolveByLayoutName(string keyboardLayoutName);

        KeyboardLayout ResolveByCulture(CultureInfo culture);

        void Activate(KeyboardLayout layout);

        void Activate(CultureInfo cultureInfo);
        
        ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }
    }
}