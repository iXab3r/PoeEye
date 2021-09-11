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
        
        /// <summary>
        ///   Resolved layout by looking for layout with specific culture(e.g. en-US, en-UK) or more generic if specific was not found (e.g. en-US will match en-GB)
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        KeyboardLayout ResolveByCulture(CultureInfo cultureInfo);

        void Activate(KeyboardLayout layout);

        ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }
    }
}