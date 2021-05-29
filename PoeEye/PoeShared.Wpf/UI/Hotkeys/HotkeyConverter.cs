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
    internal sealed class HotkeyConverter : System.ComponentModel.TypeConverter, IHotkeyConverter
    {
        private static readonly HotkeyGesture NoneHotkey = new HotkeyGesture(Key.None);
        private static readonly string NoneHotkeyName = NoneHotkey.ToString();

        private readonly IDictionary<Key, string> knownSpecialKeys = new Dictionary<Key, string>();
        private readonly IDictionary<string, Key> knownSpecialKeysByName = new Dictionary<string, Key>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IDictionary<MouseButton, string> mouseKeys = new Dictionary<MouseButton, string>();
        private readonly IDictionary<string, MouseButton> mouseKeysByName = new Dictionary<string, MouseButton>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IDictionary<MouseWheelAction, string> mouseWheelEvents = new Dictionary<MouseWheelAction, string>();
        private readonly IDictionary<string, MouseWheelAction> mouseWheelEventsByName = new Dictionary<string, MouseWheelAction>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IDictionary<Key, string> knownKeys = new Dictionary<Key, string>();
        private readonly IDictionary<string, Key> knownKeysByName = new Dictionary<string, Key>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IDictionary<ModifierKeys, string> knownModifiers = new Dictionary<ModifierKeys, string>();
        private readonly IDictionary<string, ModifierKeys> knownModifiersByName = new Dictionary<string, ModifierKeys>(StringComparer.InvariantCultureIgnoreCase);

        public HotkeyConverter()
        {
            Enum
                .GetValues(typeof(Key))
                .OfType<Key>()
                .ForEach(x =>
                {
                    var keyName = x.ToString();
                    knownKeys[x] = keyName;
                    
                    knownKeysByName[keyName] = x;

                    var hotkey = new HotkeyGesture(x);
                    knownKeysByName[hotkey.ToString()] = x;
                });

            var allModifiers = Enum
                .GetValues(typeof(ModifierKeys))
                .OfType<ModifierKeys>()
                .Select(x => new {ModifierKey = x, Name = x.ToString()})
                .Concat(new[] { new { ModifierKey = ModifierKeys.Control, Name = "CTRL" } })
                .Concat(new[] { new { ModifierKey = ModifierKeys.Windows, Name = "WIN" } })
                .ToArray();

            allModifiers
                .ToVariations()
                .ForEach(x =>
                {
                    if (!x.Select(x => x.ModifierKey).IsUnique())
                    {
                        return;
                    }
                    var modifier = x.Aggregate(ModifierKeys.None, (x, newKey) => x | newKey.ModifierKey);
                    var modifiersName = x.Where(y => y.ModifierKey != ModifierKeys.None).Select(x => x.Name.ToUpper()).JoinStrings(HotkeyGesture.ModifiersDelimiter);
                    if (!knownModifiers.ContainsKey(modifier))
                    {
                        knownModifiers[modifier] = modifiersName;
                    }
                    knownModifiersByName[modifiersName] = modifier;
                });

            Enum
                .GetValues(typeof(MouseButton))
                .OfType<MouseButton>()
                .ForEach(x =>
                {
                    var mouseButtonName = "Mouse" + x.ToString();
                    mouseKeys[x] = mouseButtonName;
                    mouseKeysByName[mouseButtonName] = x;
                });

            Enum
                .GetValues(typeof(MouseWheelAction))
                .OfType<MouseWheelAction>()
                .ForEach(x =>
                {
                    var wheelName = x.ToString();
                    mouseWheelEventsByName[wheelName] = x;
                    mouseWheelEvents[x] = wheelName;
                });
            
            knownSpecialKeysByName["*"] = Key.Multiply;
            knownSpecialKeysByName["+"] = Key.OemPlus;
            knownSpecialKeysByName["="] = Key.OemPlus;
            knownSpecialKeysByName["-"] = Key.OemMinus;
            knownSpecialKeysByName["/"] = Key.Divide;
            knownSpecialKeysByName["`"] = Key.OemTilde;
            knownSpecialKeysByName["ENTER"] = Key.Return;
            knownSpecialKeysByName["Num *"] = Key.Multiply;
            knownSpecialKeysByName["Num +"] = Key.Add;
            knownSpecialKeysByName["Num -"] = Key.Subtract;
            knownSpecialKeysByName.ForEach(x => knownSpecialKeys[x.Value] = x.Key);
        }

        public string ConvertToString(HotkeyGesture hotkeyGesture)
        {
            if (hotkeyGesture == null)
            {
                return NoneHotkey.ToString();
            }

            var keys = new List<string>();
            if (hotkeyGesture.ModifierKeys != ModifierKeys.None)
            {
                knownModifiers[hotkeyGesture.ModifierKeys].AddTo(keys);
            }

            if (hotkeyGesture.Key != Key.None)
            {
                if (knownSpecialKeys.ContainsKey(hotkeyGesture.Key))
                {
                    knownSpecialKeys[hotkeyGesture.Key].AddTo(keys);
                }
                else 
                {
                    knownKeys[hotkeyGesture.Key].AddTo(keys);
                }
            }

            if (hotkeyGesture.MouseButton != null)
            {
               mouseKeys[hotkeyGesture.MouseButton.Value].AddTo(keys);
            }
            else if (hotkeyGesture.MouseWheel != MouseWheelAction.None)
            {
                mouseWheelEvents[hotkeyGesture.MouseWheel].AddTo(keys);
            }

            if (keys.Count == 0)
            {
                return NoneHotkeyName;
            }

            return string.Join(HotkeyGesture.ModifiersDelimiter, keys);
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

            var source = ((string) sourceRaw).Trim().ToUpper(culture);
            if (string.IsNullOrWhiteSpace(source))
            {
                return NoneHotkey;
            }

            string modifiersPartRaw;
            string hotkeyPartRaw;

            var nextModifier = 0;
            var modifiersPartLength = 0;
            while (nextModifier < source.Length && (nextModifier = source.IndexOf(HotkeyGesture.ModifiersDelimiter, nextModifier)) >= 0)
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

            var modifiers = string.IsNullOrEmpty(modifiersPartRaw) ? ModifierKeys.None : knownModifiersByName[modifiersPartRaw];

            if (mouseKeysByName.TryGetValue(hotkeyPartRaw, out var mouseKey))
            {
                return new HotkeyGesture(mouseKey, modifiers);
            }

            if (mouseWheelEventsByName.TryGetValue(hotkeyPartRaw, out var mouseWheel))
            {
                return new HotkeyGesture(mouseWheel, modifiers);
            }
            
            if (knownSpecialKeysByName.TryGetValue(hotkeyPartRaw, out var specialKey))
            {
                return new HotkeyGesture(specialKey, modifiers);
            }

            if (knownKeysByName.TryGetValue(hotkeyPartRaw, out var key))
            {
                return new HotkeyGesture(key, modifiers);
            }
            
            throw new ArgumentException($"Unknown key: {hotkeyPartRaw}, modifiers: {modifiers}, source string: {source}");
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

        public HotkeyGesture Convert(string value)
        {
            return ConvertFromString(value);
        }

        public string Convert(HotkeyGesture value)
        {
            return ConvertToString(value);
        }
    }
}