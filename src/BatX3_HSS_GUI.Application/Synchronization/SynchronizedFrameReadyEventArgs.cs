using BatX3_HSS_GUI.Domain.Synchronization;

namespace BatX3_HSS_GUI.Application.Synchronization
{
    public sealed class SynchronizedFrameReadyEventArgs : EventArgs
    {
        public SynchronizedFrameReadyEventArgs(SynchronizedFrame frame)
        {
            Frame = frame;
        }

        public SynchronizedFrame Frame { get; }
    }
}