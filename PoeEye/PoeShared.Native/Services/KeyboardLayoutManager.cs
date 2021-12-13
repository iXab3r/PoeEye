using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicData;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class KeyboardLayoutManager : DisposableReactiveObject, IKeyboardLayoutManager
    {
        private static readonly IFluentLog Log = typeof(KeyboardLayoutManager).PrepareLogger();

        private readonly SourceCache<KeyboardLayout, IntPtr> layoutByLocaleId = new(x => x.Handle);

        public KeyboardLayoutManager()
        {
            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(10))
                .StartWithDefault()
                .SubscribeSafe(HandleKeyboardListUpdateRequest, Log.HandleException)
                .AddTo(Anchors);

            layoutByLocaleId
                .Connect()
                .Bind(out var knownLayouts)
                .SubscribeToErrors(Log.HandleException)
                .AddTo(Anchors);
            KnownLayouts = knownLayouts;
        }

        public ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }

        public KeyboardLayout ResolveByCulture(CultureInfo cultureInfo)
        {
            var exactMatch = layoutByLocaleId.Items.FirstOrDefault(x => Equals(x.Culture.LCID, cultureInfo.LCID));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            return layoutByLocaleId.Items.FirstOrDefault(x => Equals(x.Culture.TwoLetterISOLanguageName, cultureInfo.TwoLetterISOLanguageName));
        }

        public void ActivateForWindow(KeyboardLayout layout, IWindowHandle targetWindow)
        {
            Log.Info($"Sending keyboard layout request to {layout} for window {targetWindow}");
            if (!User32.PostMessage(targetWindow.Handle, User32.WindowMessage.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, layout.Handle))
            {
                Log.Warn($"Failed to post change language request message to window {targetWindow} while trying to change layout to {layout}");
            }
            else
            {
                Log.Info($"Successfully sent language change request for window {targetWindow}, desired layout: {layout}");
            }

            Activate(layout);
        }

        public void Activate(KeyboardLayout layout)
        {
            Guard.ArgumentNotNull(() => layout);
            var before = GetCurrent();
            if (before != layout)
            {
                Log.Info($"Activating keyboard layout {layout}, current: {before}");
                InputLanguage.CurrentInputLanguage = layout.InputLanguage;
                var after = GetCurrent();
                if (after == layout)
                {
                    Log.Info($"Changed keyboard layout {before} to {after} successfully");
                }
                else
                {
                    throw new ApplicationException($"Failed to activate layout {layout}, current: {after}");
                }
            }
            else
            {
                Log.Info($"Desired keyboard layout {layout} is already selected");
            }
        }

        public KeyboardLayout GetCurrent()
        {
            return new KeyboardLayout(InputLanguage.CurrentInputLanguage);
        }

        public KeyboardLayout GetCurrent(IWindowHandle targetWindow)
        {
            var windowThread = (uint)User32.GetWindowThreadProcessId(targetWindow.Handle, out var _); 
            var keyboardLayout = UnsafeNative.GetKeyboardLayout(windowThread);

            var knownLayout = layoutByLocaleId.Lookup(keyboardLayout);
            if (!knownLayout.HasValue)
            {
                var current = GetCurrent();
                Log.Warn($"Failed to resolve keyboard layout of window {targetWindow}, returning current thread layout {current}");
                return current;
            }

            Log.Debug(() => $"Resolved keyboard layout {keyboardLayout} for window {targetWindow}");
            return knownLayout.Value;
        }

        public KeyboardLayout ResolveByLayoutName(string keyboardLayoutName)
        {
            return layoutByLocaleId.Items.FirstOrDefault(x => string.Equals(keyboardLayoutName, x.LayoutName, StringComparison.OrdinalIgnoreCase));
        }

        private void HandleKeyboardListUpdateRequest()
        {
            var layouts = new List<KeyboardLayout>();
            for (var i = 0; i < InputLanguage.InstalledInputLanguages.Count; i++)
            {
                var layout = new KeyboardLayout(InputLanguage.InstalledInputLanguages[i]);
                layouts.Add(layout);
            }

            var addedLayouts = layouts.Where(x => !layoutByLocaleId.Lookup(x.Handle).HasValue).ToArray();
            if (addedLayouts.Any())
            {
                Log.Info($"Adding new keyboard layouts from known layouts list: {addedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");
                layoutByLocaleId.AddOrUpdate(addedLayouts);
            }

            var removedLayouts = layoutByLocaleId.Items.Where(x => !layouts.Contains(x)).ToArray();
            if (removedLayouts.Any())
            {
                Log.Info($"Removing keyboard layouts from known layouts list: {removedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");
                layoutByLocaleId.RemoveKeys(removedLayouts.Select(x => x.Handle));
            }
        }
    }
}