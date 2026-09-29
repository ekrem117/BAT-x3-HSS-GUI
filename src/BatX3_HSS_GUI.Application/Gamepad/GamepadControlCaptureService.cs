namespace BatX3_HSS_GUI.Application.Gamepad
{
    public sealed class GamepadControlCaptureService
    {
        private readonly object
            _syncRoot =
                new();

        private readonly GamepadControlEdgeProcessor
            _edgeProcessor =
                new();

        private long
            _version;

        private long
            _requestSequence;

        private CaptureRequest?
            _activeRequest;

        private bool
            _neutralObserved;

        public GamepadControlCaptureSnapshot GetSnapshot()
        {
            lock (_syncRoot)
            {
                return new GamepadControlCaptureSnapshot(
                    _version,
                    _activeRequest is not null,
                    _activeRequest?.RequestedDeviceId);
            }
        }

        public Task<GamepadControl?> CaptureAsync(
            string? requestedDeviceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CaptureRequest request;

            lock (_syncRoot)
            {
                if (_activeRequest is not null)
                {
                    throw new InvalidOperationException(
                        "Başka bir gamepad kontrol ataması zaten devam ediyor.");
                }

                long requestId =
                    checked(
                        ++_requestSequence);

                request =
                    new CaptureRequest(
                        requestId,
                        NormalizeDeviceId(
                            requestedDeviceId));

                _activeRequest =
                    request;

                _neutralObserved =
                    false;

                _edgeProcessor.Reset();

                checked
                {
                    _version++;
                }
            }

            CancellationTokenRegistration registration =
                cancellationToken.Register(
                    () =>
                        Cancel(
                            request.Id));

            return AwaitCaptureAsync(
                request.Completion.Task,
                registration);
        }

        public void Cancel()
        {
            CaptureRequest? request;

            lock (_syncRoot)
            {
                request =
                    _activeRequest;
            }

            if (request is null)
            {
                return;
            }

            Cancel(
                request.Id);
        }

        public void ProcessReading(
            string deviceId,
            GamepadReadingSnapshot reading,
            GamepadSettings settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                deviceId);

            ArgumentNullException.ThrowIfNull(
                reading);

            ArgumentNullException.ThrowIfNull(
                settings);

            TaskCompletionSource<GamepadControl?>?
                completion =
                    null;

            GamepadControl?
                capturedControl =
                    null;

            lock (_syncRoot)
            {
                CaptureRequest? request =
                    _activeRequest;

                if (request is null)
                {
                    return;
                }

                if (request.RequestedDeviceId is not null &&
                    !string.Equals(
                        request.RequestedDeviceId,
                        deviceId,
                        StringComparison.Ordinal))
                {
                    return;
                }

                /*
                 * Capture başladığı anda tutulmakta olan bir button veya
                 * trigger yeni atama olarak kabul edilmez.
                 *
                 * Önce bütün gamepad input'larının neutral olduğu en az
                 * bir örnek görülmelidir.
                 */
                if (!_neutralObserved)
                {
                    if (IsNeutral(
                            reading,
                            settings))
                    {
                        _neutralObserved =
                            true;
                    }

                    _edgeProcessor.Reset();

                    return;
                }

                GamepadControlTransitions transitions =
                    _edgeProcessor.Process(
                        reading,
                        settings);

                if (transitions.Pressed.Count == 0)
                {
                    return;
                }

                /*
                 * Aynı örnekte birden fazla yeni control görülmesi
                 * belirsizdir. Hiçbiri seçilmez ve tekrar neutral
                 * beklenir.
                 */
                if (transitions.Pressed.Count != 1)
                {
                    _neutralObserved =
                        false;

                    _edgeProcessor.Reset();

                    return;
                }

                capturedControl =
                    transitions.Pressed.Single();

                if (capturedControl ==
                    GamepadControl.None)
                {
                    return;
                }

                completion =
                    request.Completion;

                _activeRequest =
                    null;

                _neutralObserved =
                    false;

                _edgeProcessor.Reset();

                checked
                {
                    _version++;
                }
            }

            completion?.TrySetResult(
                capturedControl);
        }

        private void Cancel(
            long requestId)
        {
            TaskCompletionSource<GamepadControl?>?
                completion =
                    null;

            lock (_syncRoot)
            {
                if (_activeRequest is null ||
                    _activeRequest.Id !=
                        requestId)
                {
                    return;
                }

                completion =
                    _activeRequest.Completion;

                _activeRequest =
                    null;

                _neutralObserved =
                    false;

                _edgeProcessor.Reset();

                checked
                {
                    _version++;
                }
            }

            completion.TrySetResult(
                null);
        }

        private static bool IsNeutral(
            GamepadReadingSnapshot reading,
            GamepadSettings settings)
        {
            double deadzone =
                Math.Clamp(
                    settings.Deadzone,
                    0.0,
                    1.0);

            double triggerThreshold =
                Math.Clamp(
                    settings.TriggerThreshold,
                    0.0,
                    1.0);

            return
                Math.Abs(
                    reading.LeftStickX) <=
                    deadzone &&
                Math.Abs(
                    reading.LeftStickY) <=
                    deadzone &&
                reading.LeftTrigger <
                    triggerThreshold &&
                reading.RightTrigger <
                    triggerThreshold &&
                reading.PressedControls.Count == 0;
        }

        private static string? NormalizeDeviceId(
            string? deviceId)
        {
            return string.IsNullOrWhiteSpace(
                    deviceId)
                ? null
                : deviceId;
        }

        private static async Task<GamepadControl?>
            AwaitCaptureAsync(
                Task<GamepadControl?> task,
                CancellationTokenRegistration registration)
        {
            using (registration)
            {
                return await task.ConfigureAwait(
                    false);
            }
        }

        private sealed class CaptureRequest
        {
            public CaptureRequest(
                long id,
                string? requestedDeviceId)
            {
                Id =
                    id;

                RequestedDeviceId =
                    requestedDeviceId;
            }

            public long Id
            {
                get;
            }

            public string? RequestedDeviceId
            {
                get;
            }

            public TaskCompletionSource<GamepadControl?>
                Completion
            {
                get;
            } =
                new(
                    TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}