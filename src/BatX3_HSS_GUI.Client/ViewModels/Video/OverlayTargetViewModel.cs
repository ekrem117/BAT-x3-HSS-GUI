using System.Windows.Media;

namespace BatX3_HSS_GUI.Client.ViewModels.Video
{
    public sealed record OverlayTargetViewModel
    {
        public required int TrackId { get; init; }

        public required double X { get; init; }

        public required double Y { get; init; }

        public required double Width { get; init; }

        public required double Height { get; init; }

        public required string ClassName { get; init; }

        public required string TeamName { get; init; }

        public required double Confidence { get; init; }

        public required Brush TeamBrush { get; init; }

        public required bool IsPredicted { get; init; }

        public required bool IsLocked { get; init; }

        public string Label => IsLocked
                ? $"LOCK | {ClassName} | {TeamName} | {Confidence:P0}"
                : $"{ClassName} | {TeamName} | {Confidence:P0}";

        public double StrokeThickness => IsLocked ? 4.0 : 2.0;
    }
}