namespace BatX3_HSS_GUI.Application.Video
{
    public interface IVideoFrameSource
    {
        event EventHandler<VideoFrameReceivedEventArgs>? FrameReceived;

        bool IsRunning { get; }

        int LocalPort { get; }
    }
}