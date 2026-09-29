namespace BatX3_HSS_GUI.Application.Detection
{
    public interface IDetectionFrameSource
    {
        event EventHandler<DetectionFrameReceivedEventArgs>? FrameReceived;

        bool IsRunning { get; }

        int LocalPort { get; }
    }
}