using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Client.ViewModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class GamepadStatusPresenterHostedService :
            BackgroundService
    {
        private static readonly TimeSpan
            PresentationInterval =
                TimeSpan.FromMilliseconds(
                    200);

        private readonly GamepadRuntimeState
            _runtimeState;

        private readonly SettingsViewModel
            _settingsViewModel;

        private readonly ILogger<GamepadStatusPresenterHostedService>
            _logger;

        private GamepadRuntimeSnapshot?
            _lastPublishedSnapshot;

        public GamepadStatusPresenterHostedService(
            GamepadRuntimeState runtimeState,
            SettingsViewModel settingsViewModel,
            ILogger<GamepadStatusPresenterHostedService> logger)
        {
            _runtimeState =
                runtimeState;

            _settingsViewModel =
                settingsViewModel;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                await PublishSafelyAsync(
                    stoppingToken);

                using PeriodicTimer timer =
                    new(
                        PresentationInterval);

                while (await timer.WaitForNextTickAsync(
                           stoppingToken))
                {
                    await PublishSafelyAsync(
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
        }

        private async Task PublishSafelyAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                GamepadRuntimeSnapshot snapshot =
                    _runtimeState
                        .GetSnapshot();

                if (Equals(
                        _lastPublishedSnapshot,
                        snapshot))
                {
                    return;
                }

                Dispatcher? dispatcher =
                    System.Windows.Application
                        .Current?
                        .Dispatcher;

                if (dispatcher is null)
                {
                    return;
                }

                cancellationToken
                    .ThrowIfCancellationRequested();

                await dispatcher.InvokeAsync(
                    () =>
                    {
                        _settingsViewModel
                            .ApplyGamepadRuntimeSnapshot(
                                snapshot);
                    });

                _lastPublishedSnapshot =
                    snapshot;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                /*
                 * Runtime status presenter hatası gamepad input
                 * runtime'ını veya uygulamanın diğer servislerini
                 * etkilememelidir.
                 */
                _logger.LogWarning(
                    exception,
                    "Gamepad runtime durumu UI'ya aktarılamadı.");
            }
        }
    }
}