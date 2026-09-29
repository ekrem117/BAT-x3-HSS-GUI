using System.Windows.Input;

namespace BatX3_HSS_GUI.Client.Input
{
    public static class KeyboardKeyCatalog
    {
        private static readonly HashSet<Key> ExcludedKeys =
        [
            Key.None,
            Key.System,
            Key.ImeProcessed,
            Key.DeadCharProcessed
        ];

        public static IReadOnlyList<string> GetAvailableKeys()
        {
            return Enum
                .GetValues<Key>()
                .Where(key => !ExcludedKeys.Contains(key))
                .Select(key => key.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static bool IsModifierKey(Key key)
        {
            return key is
                Key.LeftCtrl or
                Key.RightCtrl or
                Key.LeftShift or
                Key.RightShift or
                Key.LeftAlt or
                Key.RightAlt or
                Key.LWin or
                Key.RWin;
        }

        public static bool IsAssignableKey(Key key)
        {
            return !ExcludedKeys.Contains(key) && !IsModifierKey(key) && key != Key.Escape;
        }
    }
}