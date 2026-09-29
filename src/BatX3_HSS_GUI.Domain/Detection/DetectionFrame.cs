namespace BatX3_HSS_GUI.Domain.Detection
{
    public sealed record DetectionFrame
    {
        public required uint FrameId { get; init; }

        public required double Timestamp { get; init; }

        public IReadOnlyList<DetectionTarget> Detections { get; init; } = Array.Empty<DetectionTarget>();

        public DetectionLockInfo? Lock { get; init; }
    }
}