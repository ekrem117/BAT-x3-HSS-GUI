using BatX3_HSS_GUI.Application.Gamepad;

namespace BatX3_HSS_GUI.Application.Configuration.Input
{
    public sealed class InputSettings
    {
        public const string SectionName = "Input";

        public List<ShortcutBindingSettings> Shortcuts { get; set; } = [];

        public GamepadSettings Gamepad { get; set; } = new();
    }
}