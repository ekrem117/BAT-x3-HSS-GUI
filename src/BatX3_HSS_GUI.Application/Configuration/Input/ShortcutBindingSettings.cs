namespace BatX3_HSS_GUI.Application.Configuration.Input
{
    public sealed class ShortcutBindingSettings
    {
        public string ActionId { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public ShortcutModifiers Modifiers { get; set; }
    }
}