using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public class HotkeyConverter : System.ComponentModel.TypeConverter, IHotkeyConverter
    {
        private const char ModifiersDelimiter = '+';
        private static readonly ModifierKeysConverter ModifierKeysConverter = new ModifierKeysConverter();
        private static readonly HotkeyGesture NoneHotkey = new HotkeyGesture(Key.None); 

        private readonly IDictionary<string, Key> knownSpecialKeys = new Dictionary<string, Key>();
        private readonly IDictionary<string, HotkeyGesture> mouseKeys;
        private readonly IDictionary<string, MouseWheelAction> mouseWheelEvents;
        private readonly IDictionary<string, Key> knownKeys = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);

        public HotkeyConverter()
        {
            Enum
                .GetValues(typeof(Key))
                .OfType<Key>()
                .ForEach(x => knownKeys[new HotkeyGesture(x).ToString()] = x);
            
            mouseKeys = Enum
                .GetValues(typeof(MouseButton))
                .OfType<MouseButton>()
                .Select(x => new HotkeyGesture(x))
                .ToDictionary(x => x.ToString(), x => x, StringComparer.OrdinalIgnoreCase);
            
            mouseWheelEvents = Enum
                .GetValues(typeof(MouseWheelAction))
                .OfType<MouseWheelAction>()
                .ToDictionary(x => x.ToString(), x => x, StringComparer.OrdinalIgnoreCase);
            
            knownSpecialKeys["*"] = Key.Multiply;
            knownSpecialKeys["+"] = Key.OemPlus;
            knownSpecialKeys["="] = Key.OemPlus;
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

            var source = ((string) sourceRaw).Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(source))
            {
                return NoneHotkey;
            }
            if (knownSpecialKeys.TryGetValue(source, out var specialKey))
            {
                return new HotkeyGesture(specialKey);
            }

            string modifiersPartRaw;
            string hotkeyPartRaw;

            var nextModifier = 0;
            var modifiersPartLength = 0;
            while (nextModifier < source.Length && (nextModifier = source.IndexOf(ModifiersDelimiter, nextModifier)) >= 0)
            {
                if (source.Length - (nextModifier + 1) <= 0)
                {
                    break;
                }
                modifiersPartLength = nextModifier;
                nextModifier++;
            }
            
            if (modifiersPartLength > 0)
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

            if (mouseKeys.TryGetValue(hotkeyPartRaw, out var mouseKey) && mouseKey.MouseButton != null)
            {
                return new HotkeyGesture(mouseKey.MouseButton.Value, modifiers);
            }

            if (mouseWheelEvents.TryGetValue(hotkeyPartRaw, out var mouseWheel))
            {
                return new HotkeyGesture(mouseWheel, modifiers);
            }

            if (!knownKeys.TryGetValue(hotkeyPartRaw, out var key))
            {
                return NoneHotkey;
            }

            return new HotkeyGesture(key, modifiers);
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