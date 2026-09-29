using BatX3_HSS_GUI.Domain.Video;

namespace BatX3_HSS_GUI.Application.Video
{
    public sealed class VideoFrameReceivedEventArgs : EventArgs
    {
        public VideoFrameReceivedEventArgs(VideoFrame frame)
        {
            Frame = frame;
        }

        public VideoFrame Frame { get; }
    }
}