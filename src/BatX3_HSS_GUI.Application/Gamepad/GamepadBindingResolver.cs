namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadBindingResolver
    {
        public bool TryResolve(GamepadSettings settings, GamepadControl control, out string? actionId)
        {
            ArgumentNullException.ThrowIfNull(settings);

            GamepadBindingSettings? binding = settings.Bindings.FirstOrDefault(item => item.Control == control);

            if (binding is null || string.IsNullOrWhiteSpace(binding.ActionId))
            {
                actionId = null;
                return false;
            }

            actionId = binding.ActionId;
            return true;
        }
    }
}