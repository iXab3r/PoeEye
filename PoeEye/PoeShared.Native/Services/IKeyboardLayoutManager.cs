using System.Collections.ObjectModel;
using System.Globalization;
using PoeShared.Native;

namespace PoeShared.Services
{
    public interface IKeyboardLayoutManager
    {
        /// <summary>
        ///   Uses GetKeyboardLayout(), must be called from thread with message pump, otherwise it won't work as expected
        /// </summary>
        /// <returns></returns>
        KeyboardLayout GetCurrent();

        /// <summary>
        ///   Uses GetKeyboardLayout for specified window thread, must be called from thread with message pump, otherwise it won't work as expected
        /// </summary>
        /// <param name="targetWindow"></param>
        /// <returns></returns>
        KeyboardLayout GetCurrent(IWindowHandle targetWindow);

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

        /// <summary>
        ///   Activates layout for entire system using ActivateKeyboardLayout API. Has some bugs when trying to executed this from non-ui thread (STA or message-pump?)
        /// </summary>
        /// <param name="layout"></param>
        void Activate(KeyboardLayout layout);

        /// <summary>
        ///   Works much more reliable than ActivateKeyboardLayout 
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="targetWindow"></param>
        void ActivateForWindow(KeyboardLayout layout, IWindowHandle targetWindow);

        ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }
    }
}