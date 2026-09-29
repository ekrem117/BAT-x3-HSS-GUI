namespace BatX3_HSS_GUI.Application.Diagnostics
{
    public sealed record ChannelHealthSnapshot
    {
        public required DiagnosticChannel Channel { get; init; }

        public required ChannelHealthState State { get; init; }

        public required bool IsRunning { get; init; }

        public DateTimeOffset? LastSuccessUtc { get; init; }

        public DateTimeOffset? LastErrorUtc { get; init; }

        public TimeSpan? LastSuccessAge { get; init; }

        public string? LastError { get; init; }

        public long SuccessCount { get; init; }

        public long ErrorCount { get; init; }

        public TimeSpan StaleAfter { get; init; }
    }
}