namespace BatX3_HSS_GUI.Domain.Detection
{
    public sealed record DetectionLockInfo
    {
        public int? TrackId { get; init; }

        public string? State { get; init; }

        public string? StateName { get; init; }

        public int? ClassId { get; init; }

        public int? TeamId { get; init; }

        public double? X { get; init; }

        public double? Y { get; init; }

        public double? VelocityX { get; init; }

        public double? VelocityY { get; init; }

        public double? LostFor { get; init; }

        public int? ShotsFired { get; init; }

        public int? Reacquires { get; init; }

        public double? Duration { get; init; }
    }
}