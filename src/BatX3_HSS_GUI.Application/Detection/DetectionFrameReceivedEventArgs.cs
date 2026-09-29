using BatX3_HSS_GUI.Domain.Detection;

namespace BatX3_HSS_GUI.Application.Detection
{
    public sealed class DetectionFrameReceivedEventArgs : EventArgs
    {
        public DetectionFrameReceivedEventArgs(DetectionFrame frame)
        {
            Frame = frame;
        }

        public DetectionFrame Frame { get; }
    }
}