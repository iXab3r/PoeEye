using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using log4net;
using PoeShared.UI;

namespace PoeShared.Wpf.Services
{
    internal sealed class UserInputFilterConfigurator : IUserInputFilterConfigurator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UserInputFilterConfigurator));

        private readonly ConcurrentDictionary<HotkeyGesture, int> whitelist = new();
        
        public IDisposable AddToWhitelist([NotNull] HotkeyGesture hotkey)
        {
            if (hotkey == null)
            {
                throw new ArgumentNullException(nameof(hotkey));
            }

            lock (whitelist)
            {
                whitelist.AddOrUpdate(hotkey, key =>
                {
                    Log.Debug($"Adding {key} to whitelist");
                    return 1;
                }, (key, usages) =>
                {
                    Log.Debug($"Incrementing usages of hotkey {key} {usages} => {usages + 1}");
                    return usages + 1;
                });
            }

            return Disposable.Create(() =>
            {
                lock (whitelist)
                {
                    if (!whitelist.TryGetValue(hotkey, out var usages))
                    {
                        throw new ApplicationException($"Failed to release hotkey {hotkey} from whitelist");
                    }

                    switch (usages)
                    {
                        case 1:
                        {
                            Log.Debug($"Removing hotkey {hotkey} from whitelist");
                            if (!whitelist.TryRemove(hotkey, out var _))
                            {
                                throw new ApplicationException($"Failed to remove hotkey {hotkey} from whitelist");
                            }

                            break;
                        }
                        case > 1:
                            Log.Debug($"Decrementing usages of hotkey {hotkey} {usages} => {usages - 1}");
                            whitelist[hotkey] = usages - 1;
                            break;
                        default:
                            throw new ApplicationException($"Invalid usages count for {hotkey} in whitelist: {usages} usages");
                    }
                }
            });
        }

        public bool IsInWhitelist(HotkeyGesture hotkey)
        {
            return whitelist.TryGetValue(hotkey, out var _);
        }
    }
}