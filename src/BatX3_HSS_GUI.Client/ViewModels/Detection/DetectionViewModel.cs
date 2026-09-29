using BatX3_HSS_GUI.Domain.Detection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Detection
{
    public partial class DetectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private uint _frameId;

        [ObservableProperty]
        private int _detectionCount;

        [ObservableProperty]
        private string _lockState = "-";

        [ObservableProperty]
        private int? _lockedTrackId;

        [ObservableProperty]
        private string _statusText = "Detection bekleniyor...";

        public void ApplyFrame(DetectionFrame frame)
        {
            FrameId = frame.FrameId;

            DetectionCount = frame.Detections.Count;

            LockState = frame.Lock?.StateName ?? frame.Lock?.State ?? "-";

            LockedTrackId = frame.Lock?.TrackId;

            StatusText = $"Frame {FrameId} - {DetectionCount} detection";
        }
    }
}