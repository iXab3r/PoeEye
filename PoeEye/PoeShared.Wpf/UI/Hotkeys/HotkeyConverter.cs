using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using log4net;

namespace PoeShared.UI.Hotkeys
{
    public class HotkeyConverter : System.ComponentModel.TypeConverter, IHotkeyConverter
    {
        private const char ModifiersDelimiter = '+';
        private static readonly KeyConverter KeyConverter = new KeyConverter();
        private static readonly ModifierKeysConverter ModifierKeysConverter = new ModifierKeysConverter();
        private static readonly HotkeyGesture NoneHotkey = new HotkeyGesture(Key.None); 

        private readonly IDictionary<string, HotkeyGesture> mouseKeys;
        private readonly CultureInfo[] knownCultures;
        private readonly IDictionary<string, Key> knownSpecialKeys = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);

        public HotkeyConverter()
        {
            mouseKeys = Enum
                .GetValues(typeof(MouseButton))
                .OfType<MouseButton>()
                .Select(x => new HotkeyGesture(x))
                .ToDictionary(x => x.ToString(), x => x, StringComparer.OrdinalIgnoreCase);

            this.knownCultures = new[]
            {
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("en-GB"),
                CultureInfo.GetCultureInfo("ru-RU"),
                CultureInfo.CurrentCulture,
                CultureInfo.CurrentUICulture,
                CultureInfo.InvariantCulture,
                CultureInfo.InstalledUICulture,
                CultureInfo.DefaultThreadCurrentCulture,
                CultureInfo.DefaultThreadCurrentUICulture,
            }.Where(x => x != null).Distinct().ToArray();

            knownSpecialKeys["`"] = Key.OemTilde;
        }

        public string ConvertToString(HotkeyGesture hotkeyGesture)
        {
            return hotkeyGesture == null ? NoneHotkey.ToString() : (string)ConvertTo(hotkeyGesture, typeof(string)) ?? NoneHotkey.ToString();
        }
        
        public new HotkeyGesture ConvertFromString(string source)
        {
            return (HotkeyGesture) ConvertFrom(source ?? string.Empty) ?? NoneHotkey;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType != typeof(string) || context?.Instance == null)
            {
                return false;
            }

            if (context.Instance is HotkeyGesture instance && ModifierKeysConverter.IsDefinedModifierKeys(instance.ModifierKeys))
            {
                return IsDefinedKey(instance.Key);
            }

            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object sourceRaw)
        {
            if (!(sourceRaw is string))
            {
                throw GetConvertFromException(sourceRaw);
            }

            var source = ((string) sourceRaw).Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                return new HotkeyGesture(Key.None);
            }

            var modifiersPartLength = source.LastIndexOf(ModifiersDelimiter);
            string modifiersPartRaw;
            string hotkeyPartRaw;
            if (modifiersPartLength >= 0)
            {
                modifiersPartRaw = source.Substring(0, modifiersPartLength);
                hotkeyPartRaw = source.Substring(modifiersPartLength + 1);
            }
            else
            {
                modifiersPartRaw = string.Empty;
                hotkeyPartRaw = source;
            }

            var modifiersRaw = ModifierKeysConverter.ConvertFrom(context, culture, modifiersPartRaw);
            var modifiers = (ModifierKeys) modifiersRaw;

            if (mouseKeys.ContainsKey(hotkeyPartRaw))
            {
                var mouseKey = mouseKeys[hotkeyPartRaw];
                return new HotkeyGesture(mouseKey.MouseButton.Value, modifiers);
            }

            var keyParses = new[] { culture }.Concat(this.knownCultures)
                .Distinct()
                .ToDictionary(x => x, x => ConvertFromKeySafe(context, x, hotkeyPartRaw));

            var key = keyParses.Values.FirstOrDefault(x => x != null);
            if (key == null)
            {
                throw new NotSupportedException($"Cannon convert from {sourceRaw}");
            }

            return new HotkeyGesture((Key) key, modifiers);
        }

        private object ConvertFromKeySafe(ITypeDescriptorContext context, CultureInfo culture, string source)
        {
            object key;
            try
            {
                if (knownSpecialKeys.TryGetValue(source, out var specialKey))
                {
                    return specialKey;
                }
                
                key = KeyConverter.ConvertFrom(context, culture, source);
            }
            catch (Exception)
            {
                key = null;
            }

            return key;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType != typeof(string))
            {
                throw GetConvertToException(value, destinationType);
            }

            if (value == null)
            {
                return string.Empty;
            }

            if (!(value is HotkeyGesture keyGesture))
            {
                throw GetConvertToException(value, destinationType);
            }

            return keyGesture.ToString();
        }

        private static bool IsDefinedKey(Key key)
        {
            if (key >= Key.None)
            {
                return key <= Key.OemClear;
            }

            return false;
        }
    }
}