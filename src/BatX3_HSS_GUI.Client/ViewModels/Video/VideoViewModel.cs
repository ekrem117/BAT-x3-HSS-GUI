using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Domain.Synchronization;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace BatX3_HSS_GUI.Client.ViewModels.Video
{
    public partial class VideoViewModel :
        ObservableObject
    {
        private readonly double?
            _configuredReferenceMarkerX;

        private readonly double?
            _configuredReferenceMarkerY;

        public VideoViewModel(
            ISettingsService settingsService)
        {
            ArgumentNullException.ThrowIfNull(
                settingsService);

            VideoOverlaySettings settings =
                settingsService
                    .GetVideoOverlaySettings();

            _configuredReferenceMarkerX =
                settings.ReferenceMarkerX;

            _configuredReferenceMarkerY =
                settings.ReferenceMarkerY;
        }

        [ObservableProperty]
        private ImageSource? _frame;

        [ObservableProperty]
        private uint _frameId;

        [ObservableProperty]
        private int _frameWidth =
            1280;

        [ObservableProperty]
        private int _frameHeight =
            720;

        [ObservableProperty]
        private bool _hasDetection;

        [ObservableProperty]
        private int _detectionCount;

        [ObservableProperty]
        private IReadOnlyList<OverlayTargetViewModel>
            _overlays =
                Array.Empty<OverlayTargetViewModel>();

        [ObservableProperty]
        private string _statusText =
            "Video bekleniyor...";

        public double ReferenceMarkerOffsetX
        {
            get
            {
                if (_configuredReferenceMarkerX is null)
                {
                    return 0.0;
                }

                double frameWidth =
                    Math.Max(
                        0.0,
                        FrameWidth);

                double configuredX =
                    Math.Clamp(
                        _configuredReferenceMarkerX.Value,
                        0.0,
                        frameWidth);

                return configuredX -
                       (frameWidth / 2.0);
            }
        }

        public double ReferenceMarkerOffsetY
        {
            get
            {
                if (_configuredReferenceMarkerY is null)
                {
                    return 0.0;
                }

                double frameHeight =
                    Math.Max(
                        0.0,
                        FrameHeight);

                double configuredY =
                    Math.Clamp(
                        _configuredReferenceMarkerY.Value,
                        0.0,
                        frameHeight);

                return configuredY -
                       (frameHeight / 2.0);
            }
        }

        public void ApplyFrame(
            SynchronizedFrame synchronizedFrame,
            ImageSource image,
            IReadOnlyList<OverlayTargetViewModel> overlays)
        {
            ArgumentNullException.ThrowIfNull(
                synchronizedFrame);

            ArgumentNullException.ThrowIfNull(
                image);

            ArgumentNullException.ThrowIfNull(
                overlays);

            Frame =
                image;

            FrameId =
                synchronizedFrame.Video.FrameId;

            FrameWidth =
                synchronizedFrame.Video.Width;

            FrameHeight =
                synchronizedFrame.Video.Height;

            HasDetection =
                synchronizedFrame.HasDetection;

            DetectionCount =
                synchronizedFrame
                    .Detection?
                    .Detections
                    .Count
                ?? 0;

            Overlays =
                overlays;

            if (synchronizedFrame.HasDetection)
            {
                StatusText =
                    $"Frame {FrameId} | Det: {DetectionCount}";
            }
            else
            {
                StatusText =
                    $"Frame {FrameId} | Det: --";
            }
        }

        partial void OnFrameWidthChanged(
            int value)
        {
            OnPropertyChanged(
                nameof(ReferenceMarkerOffsetX));
        }

        partial void OnFrameHeightChanged(
            int value)
        {
            OnPropertyChanged(
                nameof(ReferenceMarkerOffsetY));
        }
    }
}