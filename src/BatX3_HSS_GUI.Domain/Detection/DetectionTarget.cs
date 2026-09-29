namespace BatX3_HSS_GUI.Domain.Detection
{
    public sealed record DetectionTarget
    {
        public required int Id { get; init; }

        public required double X { get; init; }

        public required double Y { get; init; }

        public required double Width { get; init; }

        public required double Height { get; init; }

        public required int ClassId { get; init; }

        public required int TeamId { get; init; }

        public required double Confidence { get; init; }

        public int? ParentId { get; init; }

        public required double VelocityX { get; init; }

        public required double VelocityY { get; init; }

        public required double Age { get; init; }

        public required bool Predicted { get; init; }
    }
}