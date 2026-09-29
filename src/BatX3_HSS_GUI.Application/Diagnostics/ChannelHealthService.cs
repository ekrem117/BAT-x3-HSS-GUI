namespace BatX3_HSS_GUI.Application.Diagnostics
{
    public sealed class ChannelHealthService :
        IChannelHealthService
    {
        private readonly object _syncRoot =
            new();

        private readonly Dictionary<
            DiagnosticChannel,
            ChannelState> _states =
                new();

        public void ReportStarted(
            DiagnosticChannel channel,
            TimeSpan staleAfter)
        {
            if (staleAfter <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(                    nameof(staleAfter));
            }

            lock (_syncRoot)
            {
                ChannelState state =
                    GetOrCreateState(
                        channel);

                state.IsRunning =
                    true;

                state.StaleAfter =
                    staleAfter;

                state.LastError =
                    null;

                state.LastErrorUtc =
                    null;
            }
        }

        public void ReportSuccess(
            DiagnosticChannel channel)
        {
            DateTimeOffset now =
                DateTimeOffset.UtcNow;

            lock (_syncRoot)
            {
                ChannelState state =
                    GetOrCreateState(
                        channel);

                state.LastSuccessUtc =
                    now;

                state.SuccessCount++;
            }
        }

        public void ReportError(
            DiagnosticChannel channel,
            string error)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                error);

            DateTimeOffset now =
                DateTimeOffset.UtcNow;

            lock (_syncRoot)
            {
                ChannelState state =
                    GetOrCreateState(
                        channel);

                state.LastErrorUtc =
                    now;

                state.LastError =
                    error;

                state.ErrorCount++;
            }
        }

        public void ReportStopped(
            DiagnosticChannel channel)
        {
            lock (_syncRoot)
            {
                ChannelState state =
                    GetOrCreateState(
                        channel);

                state.IsRunning =
                    false;
            }
        }

        public ChannelHealthSnapshot GetSnapshot(
            DiagnosticChannel channel)
        {
            lock (_syncRoot)
            {
                ChannelState state =
                    GetOrCreateState(
                        channel);

                return CreateSnapshot(
                    channel,
                    state,
                    DateTimeOffset.UtcNow);
            }
        }

        public IReadOnlyList<ChannelHealthSnapshot>
            GetAllSnapshots()
        {
            lock (_syncRoot)
            {
                DateTimeOffset now =
                    DateTimeOffset.UtcNow;

                return Enum
                    .GetValues<DiagnosticChannel>()
                    .Select(
                        channel =>
                            CreateSnapshot(
                                channel,
                                GetOrCreateState(channel),
                                now))
                    .ToArray();
            }
        }

        private ChannelState GetOrCreateState(
            DiagnosticChannel channel)
        {
            if (_states.TryGetValue(
                    channel,
                    out ChannelState? state))
            {
                return state;
            }

            state =
                new ChannelState();

            _states.Add(
                channel,
                state);

            return state;
        }

        private static ChannelHealthSnapshot CreateSnapshot(
            DiagnosticChannel channel,
            ChannelState state,
            DateTimeOffset now)
        {
            TimeSpan? successAge =
                state.LastSuccessUtc is null
                    ? null
                    : now - state.LastSuccessUtc.Value;

            ChannelHealthState healthState =
                DetermineHealthState(
                    state,
                    successAge);

            return new ChannelHealthSnapshot
            {
                Channel =
                    channel,

                State =
                    healthState,

                IsRunning =
                    state.IsRunning,

                LastSuccessUtc =
                    state.LastSuccessUtc,

                LastErrorUtc =
                    state.LastErrorUtc,

                LastSuccessAge =
                    successAge,

                LastError =
                    state.LastError,

                SuccessCount =
                    state.SuccessCount,

                ErrorCount =
                    state.ErrorCount,

                StaleAfter =
                    state.StaleAfter
            };
        }

        private static ChannelHealthState DetermineHealthState(
            ChannelState state,
            TimeSpan? successAge)
        {
            if (!state.IsRunning)
            {
                return ChannelHealthState.Stopped;
            }

            if (state.LastSuccessUtc is null)
            {
                return ChannelHealthState.Waiting;
            }

            if (successAge > state.StaleAfter)
            {
                return ChannelHealthState.Stale;
            }

            if (state.LastErrorUtc is not null &&
                state.LastErrorUtc > state.LastSuccessUtc)
            {
                return ChannelHealthState.Degraded;
            }

            return ChannelHealthState.Healthy;
        }

        private sealed class ChannelState
        {
            public bool IsRunning { get; set; }

            public DateTimeOffset? LastSuccessUtc { get; set; }

            public DateTimeOffset? LastErrorUtc { get; set; }

            public string? LastError { get; set; }

            public long SuccessCount { get; set; }

            public long ErrorCount { get; set; }

            public TimeSpan StaleAfter { get; set; } =
                TimeSpan.FromSeconds(1);
        }
    }
}