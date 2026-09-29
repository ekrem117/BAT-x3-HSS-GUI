namespace BatX3_HSS_GUI.Domain.Video
{
    public sealed record VideoFrame
    {
        public required uint FrameId { get; init; }

        public required double Timestamp { get; init; }

        public required ushort Width { get; init; }

        public required ushort Height { get; init; }

        public required byte[] JpegBytes { get; init; }
    }
}