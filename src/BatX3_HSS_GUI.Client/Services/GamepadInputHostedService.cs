using BatX3_HSS_GUI.Application.Gamepad;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class GamepadInputHostedService :
            BackgroundService
    {
        private readonly GamepadInputRuntime
            _runtime;

        private readonly IGamepadSettingsStore
            _settingsStore;

        private readonly ILogger<GamepadInputHostedService>
            _logger;

        public GamepadInputHostedService(
            GamepadInputRuntime runtime,
            IGamepadSettingsStore settingsStore,
            ILogger<GamepadInputHostedService> logger)
        {
            _runtime =
                runtime;

            _settingsStore =
                settingsStore;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await _runtime.ProcessOnceAsync(
                        stoppingToken);

                    GamepadSettings settings =
                        _settingsStore
                            .GetSnapshot()
                            .Settings;

                    int delayMilliseconds =
                        Math.Clamp(
                            settings.PollingIntervalMs,
                            5,
                            100);

                    await Task.Delay(
                        delayMilliseconds,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                /*
                 * Normal host shutdown.
                 */
            }
            finally
            {
                try
                {
                    await _runtime.StopAsync(
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Gamepad runtime shutdown release işlemi başarısız.");
                }
            }
        }
    }
}