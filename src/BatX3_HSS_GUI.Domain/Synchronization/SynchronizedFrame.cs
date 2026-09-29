using BatX3_HSS_GUI.Domain.Detection;
using BatX3_HSS_GUI.Domain.Video;

namespace BatX3_HSS_GUI.Domain.Synchronization
{
    public sealed record SynchronizedFrame
    {
        public required VideoFrame Video { get; init; }

        public DetectionFrame? Detection { get; init; }

        public bool HasDetection => Detection is not null;

        public uint FrameId => Video.FrameId;
    }
}