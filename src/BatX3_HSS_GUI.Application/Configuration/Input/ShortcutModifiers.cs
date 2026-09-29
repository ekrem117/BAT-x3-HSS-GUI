namespace BatX3_HSS_GUI.Application.Configuration.Input
{
    [Flags]
    public enum ShortcutModifiers
    {
        None = 0,
        Control = 1,
        Alt = 2,
        Shift = 4,
        Windows = 8
    }
}