using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Configuration.Runtime;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Infrastructure.Communication.Command;
using BatX3_HSS_GUI.Infrastructure.Communication.Detection;
using BatX3_HSS_GUI.Infrastructure.Communication.Video;
using Microsoft.Extensions.Logging;

namespace BatX3_HSS_GUI.Infrastructure.Configuration.Runtime
{
    public sealed class RuntimeSettingsApplyService :
           IRuntimeSettingsApplyService,
           IDisposable
    {
        private readonly ISettingsService
            _settingsService;

        private readonly UdpCommandService
            _commandService;

        private readonly UdpVideoFrameSource
            _videoFrameSource;

        private readonly UdpDetectionFrameSource
            _detectionFrameSource;

        private readonly IFrameSynchronizer
            _frameSynchronizer;

        private readonly IGamepadSettingsStore
            _gamepadSettingsStore;

        private readonly ILogger<RuntimeSettingsApplyService>
            _logger;

        private readonly SemaphoreSlim
            _applyLock =
                new(1, 1);

        private bool
            _disposed;

        public RuntimeSettingsApplyService(
            ISettingsService settingsService,
            UdpCommandService commandService,
            UdpVideoFrameSource videoFrameSource,
            UdpDetectionFrameSource detectionFrameSource,
            IFrameSynchronizer frameSynchronizer,
            IGamepadSettingsStore gamepadSettingsStore,
            ILogger<RuntimeSettingsApplyService> logger)
        {
            _settingsService =
                settingsService;

            _commandService =
                commandService;

            _videoFrameSource =
                videoFrameSource;

            _detectionFrameSource =
                detectionFrameSource;

            _frameSynchronizer =
                frameSynchronizer;

            _gamepadSettingsStore =
                gamepadSettingsStore;

            _logger =
                logger;
        }

        public async Task<RuntimeSettingsApplyResult> ApplyAsync(
            NetworkSettings networkSettings,
            InputSettings inputSettings,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(
                networkSettings);

            ArgumentNullException.ThrowIfNull(
                inputSettings);

            IReadOnlyList<string> networkValidationErrors =
                NetworkSettingsValidator.Validate(
                    networkSettings);

            if (networkValidationErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        networkValidationErrors));
            }

            if (inputSettings.Gamepad is null)
            {
                throw new InvalidOperationException(
                    "Gamepad ayarları null olamaz.");
            }

            /*
             * Network reconfiguration başlatılmadan önce runtime'a
             * uygulanacak Gamepad configuration ayrıca doğrulanır.
             */
            GamepadSettingsValidator
                gamepadSettingsValidator =
                    new();

            IReadOnlyList<string> gamepadValidationErrors =
                gamepadSettingsValidator.Validate(
                    inputSettings.Gamepad);

            if (gamepadValidationErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        gamepadValidationErrors));
            }

            await _applyLock.WaitAsync(
                cancellationToken);

            try
            {
                /*
                 * Lock alındıktan sonra tekrar okuyoruz.
                 * Böylece arka arkaya iki Save işlemi aynı previous
                 * snapshot üzerinden çalışmaz.
                 */
                NetworkSettings previousNetworkSettings =
                    _settingsService.GetNetworkSettings();

                InputSettings previousInputSettings =
                    _settingsService.GetInputSettings();

                /*
                 * Persisted state ile runtime state'in teorik olarak
                 * farklı olabileceğini göz önünde tutuyoruz.
                 *
                 * Rollback için gerçek çalışan runtime snapshot'ı
                 * ayrıca korunur.
                 */
                GamepadSettings previousRuntimeGamepadSettings =
                    _gamepadSettingsStore
                        .GetSnapshot()
                        .Settings;

                bool requiresRebind =
                    NetworkSettingsComparer.RequiresRebind(
                        previousNetworkSettings,
                        networkSettings);

                if (!requiresRebind)
                {
                    await ApplyWithoutNetworkRebindAsync(
                        networkSettings,
                        inputSettings,
                        previousNetworkSettings,
                        previousInputSettings,
                        previousRuntimeGamepadSettings,
                        cancellationToken);

                    return new RuntimeSettingsApplyResult
                    {
                        NetworkReconfigured =
                            false
                    };
                }

                _logger.LogInformation(
                    "Runtime network reconfiguration başlatılıyor.");

                try
                {
                    await StopAllChannelsAsync(
                        cancellationToken);

                    /*
                     * Farklı network/config epoch'larına ait Video ve
                     * Detection frame'lerin birbirine eşleşmesi
                     * engellenir.
                     */
                    _frameSynchronizer.Reset();

                    await StartAllChannelsAsync(
                        networkSettings,
                        cancellationToken);

                    /*
                     * Yeni network runtime ayağa kalktıktan sonra önce
                     * persistence commit edilir.
                     */
                    await _settingsService.SaveSettingsAsync(
                        networkSettings,
                        inputSettings,
                        cancellationToken);

                    /*
                     * Persistence başarılı olduktan sonra gamepad runtime
                     * configuration atomic olarak görünür hale gelir.
                     *
                     * Settings aynıysa GamepadSettingsStore version
                     * artırmaz.
                     */
                    _gamepadSettingsStore.Replace(
                        inputSettings.Gamepad);

                    _logger.LogInformation(
                        "Runtime network ve input configuration " +
                        "başarıyla uygulandı.");

                    return new RuntimeSettingsApplyResult
                    {
                        NetworkReconfigured =
                            true
                    };
                }
                catch (Exception applyException)
                {
                    _logger.LogWarning(
                        applyException,
                        "Yeni runtime configuration uygulanamadı. " +
                        "Rollback başlatılıyor.");

                    bool rollbackSucceeded =
                        await TryRollbackAsync(
                            previousNetworkSettings,
                            previousInputSettings,
                            previousRuntimeGamepadSettings);

                    if (rollbackSucceeded)
                    {
                        throw new RuntimeSettingsApplyException(
                            "Yeni runtime configuration uygulanamadı. " +
                            "Önceki çalışan ayarlara başarıyla geri dönüldü.",
                            applyException,
                            rollbackSucceeded: true);
                    }

                    throw new RuntimeSettingsApplyException(
                        "Yeni runtime configuration uygulanamadı ve " +
                        "önceki çalışan ayarlara dönüş tamamlanamadı.",
                        applyException,
                        rollbackSucceeded: false);
                }
            }
            finally
            {
                _applyLock.Release();
            }
        }

        private async Task ApplyWithoutNetworkRebindAsync(
            NetworkSettings networkSettings,
            InputSettings inputSettings,
            NetworkSettings previousNetworkSettings,
            InputSettings previousInputSettings,
            GamepadSettings previousRuntimeGamepadSettings,
            CancellationToken cancellationToken)
        {
            /*
             * Input-only değişiklikte network stream'leri kesinlikle
             * durdurulmaz.
             *
             * Önce persistence commit edilir.
             */
            await _settingsService.SaveSettingsAsync(
                networkSettings,
                inputSettings,
                cancellationToken);

            try
            {
                /*
                 * Gamepad configuration aynıysa store no-op yapar.
                 *
                 * Farklıysa tek atomic snapshot swap ile sonraki
                 * gamepad cycle'a görünür olur.
                 */
                _gamepadSettingsStore.Replace(
                    inputSettings.Gamepad);
            }
            catch (Exception runtimeApplyException)
            {
                _logger.LogWarning(
                    runtimeApplyException,
                    "Input settings kaydedildi ancak gamepad runtime " +
                    "configuration uygulanamadı. Rollback başlatılıyor.");

                bool rollbackSucceeded =
                    await TryRollbackInputOnlyAsync(
                        previousNetworkSettings,
                        previousInputSettings,
                        previousRuntimeGamepadSettings);

                throw new RuntimeSettingsApplyException(
                    rollbackSucceeded
                        ? "Gamepad runtime configuration uygulanamadı. " +
                          "Önceki input ayarlarına geri dönüldü."
                        : "Gamepad runtime configuration uygulanamadı ve " +
                          "input rollback tamamlanamadı.",
                    runtimeApplyException,
                    rollbackSucceeded);
            }
        }

        private async Task StartAllChannelsAsync(
            NetworkSettings settings,
            CancellationToken cancellationToken)
        {
            /*
             * Command önce açılır.
             * Ardından video ve detection receive kanalları.
             *
             * Herhangi biri hata verirse caller rollback yapar.
             */
            await _commandService.StartWithSettingsAsync(
                settings,
                cancellationToken);

            await _videoFrameSource.StartWithSettingsAsync(
                settings,
                cancellationToken);

            await _detectionFrameSource.StartWithSettingsAsync(
                settings,
                cancellationToken);
        }

        private async Task StopAllChannelsAsync(
            CancellationToken cancellationToken)
        {
            List<Exception> exceptions =
                new();

            /*
             * Önce producers durdurulur.
             */
            try
            {
                await _detectionFrameSource.StopAsync(
                    cancellationToken);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            try
            {
                await _videoFrameSource.StopAsync(
                    cancellationToken);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            try
            {
                await _commandService.StopAsync(
                    cancellationToken);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "Bir veya daha fazla network channel durdurulamadı.",
                    exceptions);
            }
        }

        private async Task<bool> TryRollbackAsync(
            NetworkSettings previousNetworkSettings,
            InputSettings previousInputSettings,
            GamepadSettings previousRuntimeGamepadSettings)
        {
            bool networkRollbackSucceeded =
                true;

            bool settingsRollbackSucceeded =
                true;

            bool gamepadRollbackSucceeded =
                true;

            /*
             * Rollback user cancellation ile yarıda bırakılmaz.
             */
            try
            {
                await StopAllChannelsBestEffortAsync();
            }
            catch (Exception exception)
            {
                networkRollbackSucceeded =
                    false;

                _logger.LogError(
                    exception,
                    "Rollback sırasında channel stop işlemi " +
                    "tamamlanamadı.");
            }

            _frameSynchronizer.Reset();

            try
            {
                await StartAllChannelsAsync(
                    previousNetworkSettings,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                networkRollbackSucceeded =
                    false;

                _logger.LogCritical(
                    exception,
                    "Eski network runtime ayarları yeniden " +
                    "başlatılamadı.");
            }

            try
            {
                _gamepadSettingsStore.Replace(
                    previousRuntimeGamepadSettings);
            }
            catch (Exception exception)
            {
                gamepadRollbackSucceeded =
                    false;

                _logger.LogCritical(
                    exception,
                    "Eski gamepad runtime ayarları geri yüklenemedi.");
            }

            /*
             * Disk state de önceki snapshot'a döndürülür.
             */
            try
            {
                await _settingsService.SaveSettingsAsync(
                    previousNetworkSettings,
                    previousInputSettings,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                settingsRollbackSucceeded =
                    false;

                _logger.LogCritical(
                    exception,
                    "Eski settings snapshot kalıcılaştırılamadı.");
            }

            return
                networkRollbackSucceeded &&
                settingsRollbackSucceeded &&
                gamepadRollbackSucceeded;
        }

        private async Task<bool> TryRollbackInputOnlyAsync(
            NetworkSettings previousNetworkSettings,
            InputSettings previousInputSettings,
            GamepadSettings previousRuntimeGamepadSettings)
        {
            bool settingsRollbackSucceeded =
                true;

            bool gamepadRollbackSucceeded =
                true;

            /*
             * Input-only rollback'ta network kanallarına dokunulmaz.
             */
            try
            {
                _gamepadSettingsStore.Replace(
                    previousRuntimeGamepadSettings);
            }
            catch (Exception exception)
            {
                gamepadRollbackSucceeded =
                    false;

                _logger.LogCritical(
                    exception,
                    "Gamepad runtime input rollback tamamlanamadı.");
            }

            try
            {
                await _settingsService.SaveSettingsAsync(
                    previousNetworkSettings,
                    previousInputSettings,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                settingsRollbackSucceeded =
                    false;

                _logger.LogCritical(
                    exception,
                    "Input settings persistence rollback " +
                    "tamamlanamadı.");
            }

            return
                settingsRollbackSucceeded &&
                gamepadRollbackSucceeded;
        }

        private async Task StopAllChannelsBestEffortAsync()
        {
            List<Exception> exceptions =
                new();

            try
            {
                await _detectionFrameSource.StopAsync(
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            try
            {
                await _videoFrameSource.StopAsync(
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            try
            {
                await _commandService.StopAsync(
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                exceptions.Add(
                    exception);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "Rollback channel stop işlemlerinden biri " +
                    "başarısız oldu.",
                    exceptions);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(RuntimeSettingsApplyService));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed =
                true;

            _applyLock.Dispose();
        }
    }
}