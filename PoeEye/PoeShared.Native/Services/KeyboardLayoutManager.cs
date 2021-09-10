using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
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
            var layouts = UnsafeNative.GetKeyboardLayoutList().Select(x => new KeyboardLayout(x)).Where(x => x.IsValid).ToArray();
            var addedLayouts = layouts.Where(x => !layoutByLocaleId.Lookup(x.LayoutId).HasValue).ToArray();
            if (addedLayouts.Any())
            {
                Log.Info($"Adding new keyboard layouts from known layouts list: {addedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");

                foreach (var keyboardLayout in addedLayouts)
                {
                    var loadedLayout = UnsafeNative.LoadKeyboardLayout(new StringBuilder(keyboardLayout.LayoutName), UnsafeNative.KeyboardLayoutFlags.KLF_ACTIVATE);
                    if (loadedLayout != keyboardLayout.LayoutId)
                    {
                        Log.Warn($"Something went wrong - loaded locate should have same id as retrieved initially, loaded: {loadedLayout}, retrieved: {keyboardLayout}");
                    }
                }
                layoutByLocaleId.AddOrUpdateIfNeeded(addedLayouts);
            }
            
            var removedLayouts = layoutByLocaleId.Items.Where(x => !layouts.Contains(x)).ToArray();
            if (removedLayouts.Any())
            {
                Log.Info($"Removing keyboard layouts from known layouts list: {removedLayouts.DumpToString()}, known layouts: {layoutByLocaleId.Items.DumpToString()}");
                layoutByLocaleId.RemoveKeys(removedLayouts.Select(x => x.LayoutId));
            }
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

        public void Activate(CultureInfo cultureInfo)
        {
            var layout = ResolveByCulture(cultureInfo);
            Activate(layout);
        }

        public KeyboardLayout GetCurrent()
        {
            var currentThreadId = UnsafeNative.GetCurrentThreadId();
            var current = UnsafeNative.GetKeyboardLayout((uint)currentThreadId);
            var result = new KeyboardLayout(current);
            if (!result.IsValid)
            {
                throw new ApplicationException($"Failed to get current keyboard layout for thread {currentThreadId}");
            }

            return result;
        }

        public KeyboardLayout ResolveByLayoutName(string keyboardLayoutName)
        {
            return layoutByLocaleId.Items.FirstOrDefault(x => string.Equals(keyboardLayoutName, x.LayoutName, StringComparison.OrdinalIgnoreCase));
        }

        public KeyboardLayout ResolveByCulture(CultureInfo culture)
        {
            var cultureId = culture.LCID.ToString("x8");
            var keyboardLayout = UnsafeNative.LoadKeyboardLayout(new StringBuilder(cultureId), UnsafeNative.KeyboardLayoutFlags.KLF_ACTIVATE);
            var result = new KeyboardLayout(keyboardLayout);
            if (!result.IsValid)
            {
                throw new ApplicationException($"Failed to resolve keyboard layout by culture {culture}");
            }

            return result;
        }
        
        public ReadOnlyObservableCollection<KeyboardLayout> KnownLayouts { get; }
    }
}