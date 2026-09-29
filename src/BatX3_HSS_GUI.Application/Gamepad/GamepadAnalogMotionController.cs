using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadAnalogMotionController :
        IGamepadAnalogMotionController,
        IDisposable
    {
        private const double AnalogChangeEpsilon =
            0.01;

        private readonly IParameterService
            _parameterService;

        private readonly ISystemRuntimeState
            _systemRuntimeState;

        private readonly SemaphoreSlim
            _stateLock =
                new(1, 1);

        private double? _lastPan;

        private double? _lastTilt;

        private int? _lastPrecision;

        private bool _disposed;

        public GamepadAnalogMotionController(
            IParameterService parameterService,
            ISystemRuntimeState systemRuntimeState)
        {
            _parameterService =
                parameterService;

            _systemRuntimeState =
                systemRuntimeState;
        }

        public async Task<bool> ApplyAsync(
            GamepadAnalogState state,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(
                state);

            if (!state.IsReady)
            {
                return false;
            }

            /*
             * Analog gamepad hareketi yalnız MANUAL modunda
             * uygulanabilir.
             *
             * Mode transition sırasında runtime ayrıca fail-safe
             * sıfırlama yapar. Bu kontrol defense-in-depth'tir.
             */
            if (_systemRuntimeState.CurrentMode !=
                SystemOperatingMode.Manual)
            {
                return false;
            }

            double pan =
                NormalizeAnalogValue(
                    state.Pan);

            double tilt =
                NormalizeAnalogValue(
                    state.Tilt);

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                Dictionary<string, object> values =
                    new(
                        StringComparer.Ordinal);

                if (ShouldSendAnalog(
                        _lastPan,
                        pan))
                {
                    values[
                        ParameterNames.Motion.AnalogPan] =
                            pan;
                }

                if (ShouldSendAnalog(
                        _lastTilt,
                        tilt))
                {
                    values[
                        ParameterNames.Motion.AnalogTilt] =
                            tilt;
                }

                if (values.Count == 0)
                {
                    return true;
                }

                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.SetAsync(
                            values,
                            cancellationToken);

                ValidateAndTrackResults(
                    values,
                    results);

                return true;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task<bool> SetPrecisionAsync(
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            /*
             * Precision aktif etme yalnız MANUAL modunda kabul edilir.
             *
             * Precision kapatma ise her modda yapılabilir; fail-safe
             * release buna ihtiyaç duyar.
             */
            if (isActive &&
                _systemRuntimeState.CurrentMode !=
                    SystemOperatingMode.Manual)
            {
                return false;
            }

            int targetValue =
                isActive
                    ? 1
                    : 0;

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                if (_lastPrecision.HasValue &&
                    _lastPrecision.Value ==
                        targetValue)
                {
                    return true;
                }

                Dictionary<string, object> values =
                    new(
                        StringComparer.Ordinal)
                    {
                        [ParameterNames.Motion.AnalogPrecision] =
                            targetValue
                    };

                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.SetAsync(
                            values,
                            cancellationToken);

                ValidateAndTrackResults(
                    values,
                    results);

                return true;
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task ResetAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                /*
                 * Fail-safe reset CACHE'e bakmadan daima gönderilir.
                 *
                 * Client daha önce 0 gönderdiğini düşünse bile server
                 * state'i başka bir kaynaktan değişmiş olabilir.
                 */
                Dictionary<string, object> values =
                    new(
                        StringComparer.Ordinal)
                    {
                        [ParameterNames.Motion.AnalogPan] =
                            0.0,

                        [ParameterNames.Motion.AnalogTilt] =
                            0.0,

                        [ParameterNames.Motion.AnalogPrecision] =
                            0
                    };

                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.SetAsync(
                            values,
                            cancellationToken);

                ValidateAndTrackResults(
                    values,
                    results);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private void ValidateAndTrackResults(
            IReadOnlyDictionary<string, object> requestedValues,
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            List<Exception> failures =
                new();

            foreach (
                KeyValuePair<string, object> requested
                in requestedValues)
            {
                if (!results.TryGetValue(
                        requested.Key,
                        out ParameterOperationResult result))
                {
                    ClearTrackedValue(
                        requested.Key);

                    failures.Add(
                        new InvalidOperationException(
                            $"'{requested.Key}' SET cevabı bulunamadı."));

                    continue;
                }

                if (!result.IsSuccess)
                {
                    ClearTrackedValue(
                        requested.Key);

                    failures.Add(
                        new InvalidOperationException(
                            $"'{requested.Key}' SET işlemi reddedildi: " +
                            $"{result.Status}"));

                    continue;
                }

                TrackSuccessfulValue(
                    requested.Key,
                    requested.Value);
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Gamepad analog parameter SET işlemlerinden " +
                    "biri veya daha fazlası başarısız oldu.",
                    failures);
            }
        }

        private void TrackSuccessfulValue(
            string parameterName,
            object value)
        {
            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogPan,
                    StringComparison.Ordinal))
            {
                _lastPan =
                    Convert.ToDouble(
                        value);

                return;
            }

            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogTilt,
                    StringComparison.Ordinal))
            {
                _lastTilt =
                    Convert.ToDouble(
                        value);

                return;
            }

            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogPrecision,
                    StringComparison.Ordinal))
            {
                _lastPrecision =
                    Convert.ToInt32(
                        value);
            }
        }

        private void ClearTrackedValue(
            string parameterName)
        {
            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogPan,
                    StringComparison.Ordinal))
            {
                _lastPan =
                    null;

                return;
            }

            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogTilt,
                    StringComparison.Ordinal))
            {
                _lastTilt =
                    null;

                return;
            }

            if (string.Equals(
                    parameterName,
                    ParameterNames.Motion.AnalogPrecision,
                    StringComparison.Ordinal))
            {
                _lastPrecision =
                    null;
            }
        }

        private static bool ShouldSendAnalog(
            double? previousValue,
            double currentValue)
        {
            if (!previousValue.HasValue)
            {
                return true;
            }

            /*
             * Sıfıra dönüş DAİMA gönderilir.
             *
             * Bu kural epsilon bastırmasından daha önceliklidir.
             */
            if (currentValue == 0.0 &&
                previousValue.Value != 0.0)
            {
                return true;
            }

            return Math.Abs(
                       previousValue.Value -
                       currentValue) >=
                   AnalogChangeEpsilon;
        }

        private static double NormalizeAnalogValue(
            double value)
        {
            if (double.IsNaN(
                    value) ||
                double.IsInfinity(
                    value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Analog gamepad değeri sonlu olmalıdır.");
            }

            double clamped =
                Math.Clamp(
                    value,
                    -1.0,
                    1.0);

            double rounded =
                Math.Round(
                    clamped,
                    3);

            return rounded == 0.0
                ? 0.0
                : rounded;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(GamepadAnalogMotionController));
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

            _stateLock.Dispose();
        }
    }
}