namespace BatX3_HSS_GUI.Application.Gamepad
{
    public interface IGamepadAnalogMotionController
    {
        Task<bool> ApplyAsync(
            GamepadAnalogState state,
            CancellationToken cancellationToken = default);

        Task<bool> SetPrecisionAsync(
            bool isActive,
            CancellationToken cancellationToken = default);

        Task ResetAsync(
            CancellationToken cancellationToken = default);
    }
}