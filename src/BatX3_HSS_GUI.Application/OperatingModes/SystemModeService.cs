using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;
using Microsoft.Extensions.Logging;

namespace BatX3_HSS_GUI.Application.OperatingModes
{
    public sealed class SystemModeService :
            ISystemModeService
    {
        private readonly IParameterService _parameterService;
        private readonly ISystemRuntimeState _systemRuntimeState;
        private readonly IApplicationActionService _actionService;
        private readonly ILogger<SystemModeService> _logger;

        private readonly SemaphoreSlim _transitionLock =
            new(1, 1);

        public SystemModeService(
            IParameterService parameterService,
            ISystemRuntimeState systemRuntimeState,
            IApplicationActionService actionService,
            ILogger<SystemModeService> logger)
        {
            _parameterService =
                parameterService;

            _systemRuntimeState =
                systemRuntimeState;

            _actionService =
                actionService;

            _logger =
                logger;
        }

        public async Task SetModeAsync(
            SystemOperatingMode targetMode,
            CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(
                cancellationToken);

            try
            {
                SystemOperatingMode? currentMode =
                    _systemRuntimeState.CurrentMode;

                /*
                 * Runtime mode bilinmiyorsa normal bir mode transition
                 * güvenli biçimde planlanamaz.
                 *
                 * ESTOP bunun tek istisnasıdır; her moddan doğrudan
                 * girilebildiği için mevcut mode bilinmese de
                 * gönderilebilir.
                 */
                if (currentMode is null)
                {
                    if (targetMode !=
                        SystemOperatingMode.EmergencyStop)
                    {
                        throw new InvalidOperationException(
                            "Mevcut çalışma modu bilinmiyor. " +
                            "Bağlantı ve sistem durumu doğrulanmadan " +
                            "çalışma modu değiştirilemez.");
                    }

                    await ReleaseBeforeEmergencyStopAsync(
                        cancellationToken);

                    await SetModeStepAsync(
                        SystemOperatingMode.EmergencyStop,
                        cancellationToken);

                    return;
                }

                IReadOnlyList<SystemOperatingMode> transitionPath =
                    SystemModeTransitionPlanner.CreateTransitionPath(
                        currentMode.Value,
                        targetMode);

                if (transitionPath.Count == 0)
                {
                    return;
                }

                /*
                 * ESTOP normal transition zincirinden farklıdır.
                 * Aktif movement release başarısız olsa bile ESTOP
                 * SET komutu engellenmemelidir.
                 */
                if (targetMode ==
                    SystemOperatingMode.EmergencyStop)
                {
                    await ReleaseBeforeEmergencyStopAsync(
                        cancellationToken);
                }
                else
                {
                    /*
                     * Normal mode değişiminde aktif momentary
                     * hareketlerin bırakılması zorunludur.
                     */
                    await _actionService.ReleaseAllMomentaryAsync(
                        cancellationToken);
                }

                foreach (
                    SystemOperatingMode transitionMode
                    in transitionPath)
                {
                    await SetModeStepAsync(
                        transitionMode,
                        cancellationToken);
                }
            }
            finally
            {
                _transitionLock.Release();
            }
        }

        private async Task ReleaseBeforeEmergencyStopAsync(
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
                /*
                 * Release problemi ESTOP komutunu engellemez.
                 */
                _logger.LogWarning(
                    exception,
                    "ESTOP öncesi momentary action release " +
                    "tamamlanamadı. ESTOP gönderimine devam ediliyor.");
            }
        }

        private async Task SetModeStepAsync(
            SystemOperatingMode mode,
            CancellationToken cancellationToken)
        {
            ParameterOperationResult result;

            try
            {
                result =
                    await _parameterService.SetAsync(
                        ParameterNames.System.Mode,
                        (int)mode,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                /*
                 * SET gönderilmiş olabilir fakat sonucu bilinmiyor.
                 */
                _systemRuntimeState.MarkUnknown();

                throw;
            }
            catch
            {
                /*
                 * UDP timeout / belirsiz network sonucu.
                 */
                _systemRuntimeState.MarkUnknown();

                throw;
            }

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Çalışma modu değiştirilemedi. " +
                    $"Hedef: {mode}, Status: {result.Status}.");
            }

            /*
             * Server SET'i kabul etti.
             * Polling birkaç ms sonra tekrar doğrulayacaktır.
             */
            _systemRuntimeState.UpdateMode(
                mode);
        }
    }
}