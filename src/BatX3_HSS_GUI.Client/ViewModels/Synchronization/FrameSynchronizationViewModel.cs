using BatX3_HSS_GUI.Domain.Synchronization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Synchronization
{
    public partial class FrameSynchronizationViewModel : ObservableObject
    {
        [ObservableProperty]
        private uint _frameId;

        [ObservableProperty]
        private uint? _detectionFrameId;

        [ObservableProperty]
        private bool _hasDetection;

        [ObservableProperty]
        private long _matchedFrameCount;

        [ObservableProperty]
        private long _videoOnlyFrameCount;

        [ObservableProperty]
        private string _statusText = "Senkronize frame bekleniyor...";

        public void ApplyFrame(SynchronizedFrame frame)
        {
            FrameId = frame.Video.FrameId;

            DetectionFrameId = frame.Detection?.FrameId;

            HasDetection = frame.HasDetection;

            if (frame.HasDetection)
            {
                MatchedFrameCount++;

                StatusText = $"MATCH — Frame {frame.FrameId}";
            }
            else
            {
                VideoOnlyFrameCount++;

                StatusText = $"VIDEO ONLY — Frame {frame.FrameId}";
            }
        }
    }
}