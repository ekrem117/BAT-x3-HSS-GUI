using BatX3_HSS_GUI.Domain.Detection;
using BatX3_HSS_GUI.Domain.Video;

namespace BatX3_HSS_GUI.Application.Synchronization
{
    public interface IFrameSynchronizer
    {
        event EventHandler<SynchronizedFrameReadyEventArgs>? FrameReady;

        long MatchedFrameCount { get; }

        long VideoOnlyFrameCount { get; }

        long DroppedDetectionFrameCount { get; }

        void PushVideo(VideoFrame frame);

        void PushDetection(DetectionFrame frame);

        void Reset();
    }
}