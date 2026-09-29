using BatX3_HSS_GUI.Application.Actions;

namespace BatX3_HSS_GUI.Application.Input
{
    public sealed class ManualInputCoordinator :
       IManualInputCoordinator,
       IDisposable
    {
        private static readonly IReadOnlySet<string>
            SupportedActions =
                new HashSet<string>(
                    StringComparer.Ordinal)
                {
                    ApplicationActionIds.MotionUp,
                    ApplicationActionIds.MotionDown,
                    ApplicationActionIds.MotionLeft,
                    ApplicationActionIds.MotionRight
                };

        private readonly IManualInputActionSink
            _actionSink;

        private readonly SemaphoreSlim
            _stateLock =
                new(1, 1);

        private readonly Dictionary<
            string,
            ManualActionState> _states =
                new(
                    StringComparer.Ordinal);

        private bool _disposed;

        public ManualInputCoordinator(
            IManualInputActionSink actionSink)
        {
            _actionSink =
                actionSink;

            foreach (
                string actionId
                in SupportedActions)
            {
                _states[actionId] =
                    new ManualActionState();
            }
        }

        public async Task SetActiveAsync(
            ManualInputSource source,
            string actionId,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ValidateSource(
                source);

            ValidateAction(
                actionId);

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                ManualActionState state =
                    _states[actionId];

                bool sourceStateChanged;

                if (isActive)
                {
                    sourceStateChanged =
                        state.ActiveSources.Add(
                            source);
                }
                else
                {
                    sourceStateChanged =
                        state.ActiveSources.Remove(
                            source);
                }

                if (!sourceStateChanged &&
                    state.DispatchedActive.HasValue)
                {
                    return;
                }

                await SynchronizeActionAsync(
                    actionId,
                    state,
                    cancellationToken);
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task ReleaseSourceAsync(
            ManualInputSource source,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            ValidateSource(
                source);

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                List<Exception> exceptions =
                    new();

                foreach (
                    KeyValuePair<string, ManualActionState>
                    entry
                    in _states)
                {
                    bool removed =
                        entry.Value.ActiveSources.Remove(
                            source);

                    if (!removed &&
                        entry.Value.DispatchedActive.HasValue)
                    {
                        continue;
                    }

                    try
                    {
                        await SynchronizeActionAsync(
                            entry.Key,
                            entry.Value,
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(
                            exception);
                    }
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(
                        $"'{source}' kaynağı için bir veya daha fazla " +
                        "manuel hareket fail-safe release edilemedi.",
                        exceptions);
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task ReleaseAllAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _stateLock.WaitAsync(
                cancellationToken);

            try
            {
                List<Exception> exceptions =
                    new();

                foreach (
                    KeyValuePair<string, ManualActionState>
                    entry
                    in _states)
                {
                    entry.Value.ActiveSources.Clear();

                    try
                    {
                        await SynchronizeActionAsync(
                            entry.Key,
                            entry.Value,
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        exceptions.Add(
                            exception);
                    }
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(
                        "Bir veya daha fazla manuel hareket " +
                        "fail-safe release edilemedi.",
                        exceptions);
                }
            }
            finally
            {
                _stateLock.Release();
            }
        }

        private async Task SynchronizeActionAsync(
            string actionId,
            ManualActionState state,
            CancellationToken cancellationToken)
        {
            bool desiredActive =
                state.ActiveSources.Count > 0;

            if (state.DispatchedActive.HasValue &&
                state.DispatchedActive.Value ==
                    desiredActive)
            {
                return;
            }

            try
            {
                if (desiredActive)
                {
                    await _actionSink.PressAsync(
                        actionId,
                        cancellationToken);
                }
                else
                {
                    await _actionSink.ReleaseAsync(
                        actionId,
                        cancellationToken);
                }

                state.DispatchedActive =
                    desiredActive;
            }
            catch
            {
                state.DispatchedActive =
                    null;

                throw;
            }
        }

        private static void ValidateAction(
            string actionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                actionId);

            if (!SupportedActions.Contains(
                    actionId))
            {
                throw new ArgumentException(
                    $"ManualInputCoordinator yalnız manuel hareket " +
                    $"action'larını kabul eder: {actionId}",
                    nameof(actionId));
            }
        }

        private static void ValidateSource(
            ManualInputSource source)
        {
            if (!Enum.IsDefined(
                    source))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    source,
                    "Bilinmeyen manuel input kaynağı.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(ManualInputCoordinator));
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

        private sealed class ManualActionState
        {
            public HashSet<ManualInputSource>
                ActiveSources
            {
                get;
            } =
                new();

            public bool? DispatchedActive
            {
                get;
                set;
            } =
                false;
        }
    }
}