using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Communication.Command.Exceptions;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.Polling;
using BatX3_HSS_GUI.Client.ViewModels.Operation;
using BatX3_HSS_GUI.Client.ViewModels.Parameters;
using BatX3_HSS_GUI.Domain.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Windows.Threading;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class ParameterPollingHostedService :
        BackgroundService
    {
        private static readonly TimeSpan PollingInterval =
            TimeSpan.FromMilliseconds(
                50);

        private readonly IParameterService _parameterService;
        private readonly ISystemRuntimeState _systemRuntimeState;
        private readonly IApplicationActionService _actionService;
        private readonly ParametersViewModel _parametersViewModel;
        private readonly OperationViewModel _operationViewModel;
        private readonly ILogger<ParameterPollingHostedService> _logger;

        public ParameterPollingHostedService(
            IParameterService parameterService,
            ISystemRuntimeState systemRuntimeState,
            IApplicationActionService actionService,
            ParametersViewModel parametersViewModel,
            OperationViewModel operationViewModel,
            ILogger<ParameterPollingHostedService> logger)
        {
            _parameterService =
                parameterService;

            _systemRuntimeState =
                systemRuntimeState;

            _actionService =
                actionService;

            _parametersViewModel =
                parametersViewModel;

            _operationViewModel =
                operationViewModel;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using PeriodicTimer timer =
                new(
                    PollingInterval);

            await PollOnceAsync(
                stoppingToken);

            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await PollOnceAsync(
                    stoppingToken);
            }
        }

        private async Task PollOnceAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.GetAsync(
                            ParameterPollingProfile.StatusParameters,
                            cancellationToken);

                await UpdateSystemRuntimeStateAsync(
                    results,
                    cancellationToken);

                await ApplyResultsAsync(
                    results,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                /*
                 * Host kapanışı.
                 */
            }
            catch (CommandTimeoutException exception)
            {
                _logger.LogDebug(
                    exception,
                    "Parameter polling command timeout.");

                await MarkOperationStatusUnavailableAsync(
                    cancellationToken);

                await HandleRuntimeStateUnavailableAsync(
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Parameter polling cycle başarısız.");

                await MarkOperationStatusUnavailableAsync(
                    cancellationToken);

                await HandleRuntimeStateUnavailableAsync(
                    cancellationToken);
            }
        }

        private async Task UpdateSystemRuntimeStateAsync(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            CancellationToken cancellationToken)
        {
            if (!TryGetOperatingMode(
                    results,
                    out SystemOperatingMode mode))
            {
                _systemRuntimeState.MarkUnknown();

                _logger.LogWarning(
                    "system.mode polling sonucu geçersiz veya kullanılamıyor. " +
                    "Runtime sistem modu bilinmiyor olarak işaretlendi.");

                await ReleaseMomentaryActionsSafelyAsync(
                    cancellationToken);

                return;
            }

            _systemRuntimeState.UpdateMode(
                mode);

            if (mode != SystemOperatingMode.Manual)
            {
                await ReleaseMomentaryActionsSafelyAsync(
                    cancellationToken);
            }
        }

        private async Task HandleRuntimeStateUnavailableAsync(
            CancellationToken cancellationToken)
        {
            _systemRuntimeState.MarkUnknown();

            await ReleaseMomentaryActionsSafelyAsync(
                cancellationToken);
        }

        private async Task ReleaseMomentaryActionsSafelyAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await _actionService.ReleaseAllMomentaryAsync(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Aktif momentary action'ların fail-safe release " +
                    "işlemi tamamlanamadı.");
            }
        }

        private static bool TryGetOperatingMode(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            out SystemOperatingMode mode)
        {
            mode =
                default;

            if (!results.TryGetValue(
                    ParameterNames.System.Mode,
                    out ParameterOperationResult modeResult))
            {
                return false;
            }

            try
            {
                int status =
                    Convert.ToInt32(
                        modeResult.Status,
                        CultureInfo.InvariantCulture);

                if (status != 0 ||
                    modeResult.Value is null)
                {
                    return false;
                }

                int modeValue =
                    Convert.ToInt32(
                        modeResult.Value,
                        CultureInfo.InvariantCulture);

                if (!Enum.IsDefined(
                        typeof(SystemOperatingMode),
                        modeValue))
                {
                    return false;
                }

                mode =
                    (SystemOperatingMode)modeValue;

                return true;
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                return false;
            }
        }

        private async Task ApplyResultsAsync(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results,
            CancellationToken cancellationToken)
        {
            Dispatcher? dispatcher =
                System.Windows.Application.Current?.Dispatcher;

            if (dispatcher is null)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await dispatcher.InvokeAsync(
                () =>
                {
                    _parametersViewModel.ApplyPollingResults(
                        results);

                    _operationViewModel.ApplyPollingResults(
                        results);
                });
        }

        private async Task MarkOperationStatusUnavailableAsync(
            CancellationToken cancellationToken)
        {
            Dispatcher? dispatcher =
                System.Windows.Application.Current?.Dispatcher;

            if (dispatcher is null)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await dispatcher.InvokeAsync(
                () =>
                {
                    _operationViewModel.MarkUnavailable();
                });
        }
    }
}