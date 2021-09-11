using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class KeyboardLayoutManager : DisposableReactiveObject, IKeyboardLayoutManager
    {
        private static readonly IFluentLog Log = typeof(KeyboardLayoutManager).PrepareLogger();

        private readonly SourceCache<KeyboardLayout, uint> layoutByLocaleId = new(x => x.LayoutId);

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

        private void HandleKeyboardListUpdateRequest()
        {
            var layouts = new List<KeyboardLayout>();
            for (var i = 0; i < InputLanguage.InstalledInputLanguages.Count; i++)
            {
                var layout = new KeyboardLayout(InputLanguage.InstalledInputLanguages[i]);
                layouts.Add(layout);
            }
            
            var addedLayouts = layouts.Where(x => !layoutByLocaleId.Lookup(x.LayoutId).HasValue).ToArray();
            if (addedLayouts.Any())
            {
                Log.Info($"Adding new keyboard layouts from known layouts list: {addedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");
                layoutByLocaleId.AddOrUpdateIfNeeded(addedLayouts);
            }
            
            var removedLayouts = layoutByLocaleId.Items.Where(x => !layouts.Contains(x)).ToArray();
            if (removedLayouts.Any())
            {
                Log.Info($"Removing keyboard layouts from known layouts list: {removedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");
                layoutByLocaleId.RemoveKeys(removedLayouts.Select(x => x.LayoutId));
            }
        }

        public KeyboardLayout ResolveByCulture(CultureInfo cultureInfo)
        {
            var exactMatch = layoutByLocaleId.Items.FirstOrDefault(x => Equals(x.Culture.LCID, cultureInfo.LCID));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            return layoutByLocaleId.Items.FirstOrDefault(x => Equals(x.Culture.TwoLetterISOLanguageName, cultureInfo.TwoLetterISOLanguageName));
        }

        public void Activate(KeyboardLayout layout)
        {
            Guard.ArgumentNotNull(() => layout);
            var before = GetCurrent();
            Log.Info($"Activating keyboard layout {layout}, current: {before}");
            UnsafeNative.ActivateKeyboardLayout(layout.LayoutId, UnsafeNative.KeyboardLayoutFlags.KLF_SETFORPROCESS);
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

        public KeyboardLayout GetCurrent()
        {
            var result = new KeyboardLayout(InputLanguage.CurrentInputLanguage);
            if (!result.IsValid)
            {
                throw new ApplicationException($"Failed to get current keyboard layout");
            }

            return result;
        }

        public KeyboardLayout ResolveByLayoutName(string keyboardLayoutName)
        {
            return layoutByLocaleId.Items.FirstOrDefault(x => string.Equals(keyboardLayoutName, x.LayoutName, StringComparison.OrdinalIgnoreCase));
        }
        
        public ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }
    }
}