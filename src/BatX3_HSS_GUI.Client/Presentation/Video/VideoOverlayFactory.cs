using BatX3_HSS_GUI.Client.ViewModels.Video;
using BatX3_HSS_GUI.Domain.Detection;
using System.Windows.Media;

namespace BatX3_HSS_GUI.Client.Presentation.Video
{
    public static class VideoOverlayFactory
    {
        public static IReadOnlyList<OverlayTargetViewModel> Create(
            DetectionFrame? detectionFrame,
            int videoWidth,
            int videoHeight)
        {
            if (detectionFrame is null)
            {
                return Array.Empty<OverlayTargetViewModel>();
            }

            if (videoWidth <= 0 ||
                videoHeight <= 0)
            {
                return Array.Empty<OverlayTargetViewModel>();
            }

            double scaleX =
                videoWidth /
                (double)DetectionCoordinateSpaceProfile.SourceWidth;

            double scaleY =
                videoHeight /
                (double)DetectionCoordinateSpaceProfile.SourceHeight;

            List<OverlayTargetViewModel> overlays =
                new(
                    detectionFrame.Detections.Count);

            foreach (
                DetectionTarget target
                in detectionFrame.Detections)
            {
                bool isLocked =
                    detectionFrame.Lock?.TrackId ==
                    target.Id;

                overlays.Add(
                    new OverlayTargetViewModel
                    {
                        TrackId =
                            target.Id,

                        X =
                            target.X * scaleX,

                        Y =
                            target.Y * scaleY,

                        Width =
                            target.Width * scaleX,

                        Height =
                            target.Height * scaleY,

                        ClassName =
                            GetClassName(
                                target.ClassId),

                        TeamName =
                            GetTeamName(
                                target.TeamId),

                        Confidence =
                            target.Confidence,

                        TeamBrush =
                            GetTeamBrush(
                                target.TeamId),

                        IsPredicted =
                            target.Predicted,

                        IsLocked =
                            isLocked
                    });
            }

            return overlays;
        }

        private static string GetClassName(
            int classId)
        {
            return classId switch
            {
                0 => "Drone / Mini-Micro İHA",
                1 => "F-16",
                2 => "Balistik Füze",
                3 => "Helikopter",
                4 => "Balon",
                _ => $"Sınıf {classId}"
            };
        }

        private static string GetTeamName(
            int teamId)
        {
            return teamId switch
            {
                0 => "Dost",
                1 => "Düşman",
                2 => "Bilinmiyor",
                _ => $"Takım {teamId}"
            };
        }

        private static Brush GetTeamBrush(
            int teamId)
        {
            return teamId switch
            {
                0 => Brushes.LimeGreen,
                1 => Brushes.Red,
                2 => Brushes.Yellow,
                _ => Brushes.White
            };
        }
    }
}