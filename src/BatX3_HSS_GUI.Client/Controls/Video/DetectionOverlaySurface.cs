using BatX3_HSS_GUI.Client.ViewModels.Video;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BatX3_HSS_GUI.Client.Controls.Video
{
    public sealed class DetectionOverlaySurface :
        FrameworkElement
    {
        private const double LabelFontSize =
            9.5;

        private const double LabelHorizontalPadding =
            4.0;

        private const double LabelVerticalPadding =
            1.5;

        private const double LabelBoxGap =
            3.0;

        private const double SurfaceEdgeMargin =
            2.0;

        private const double LabelCornerRadius =
            2.0;

        public static readonly DependencyProperty TargetsProperty =
            DependencyProperty.Register(
                nameof(Targets),
                typeof(IReadOnlyList<OverlayTargetViewModel>),
                typeof(DetectionOverlaySurface),
                new FrameworkPropertyMetadata(
                    Array.Empty<OverlayTargetViewModel>(),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IReadOnlyList<OverlayTargetViewModel> Targets
        {
            get
            {
                return
                    (IReadOnlyList<OverlayTargetViewModel>)
                    GetValue(TargetsProperty);
            }

            set
            {
                SetValue(
                    TargetsProperty,
                    value);
            }
        }

        protected override void OnRender(
            DrawingContext drawingContext)
        {
            base.OnRender(
                drawingContext);

            IReadOnlyList<OverlayTargetViewModel> targets =
                Targets;

            if (targets.Count == 0)
            {
                return;
            }

            double pixelsPerDip =
                VisualTreeHelper
                    .GetDpi(this)
                    .PixelsPerDip;

            foreach (
                OverlayTargetViewModel target
                in targets)
            {
                DrawTarget(
                    drawingContext,
                    target,
                    pixelsPerDip);
            }
        }

        private void DrawTarget(
            DrawingContext drawingContext,
            OverlayTargetViewModel target,
            double pixelsPerDip)
        {
            if (target.Width <= 0 ||
                target.Height <= 0)
            {
                return;
            }

            Rect boundingBox =
                new(
                    target.X,
                    target.Y,
                    target.Width,
                    target.Height);

            Pen pen =
                CreatePen(
                    target);

            drawingContext.DrawRectangle(
                brush: null,
                pen,
                boundingBox);

            DrawLabel(
                drawingContext,
                target,
                boundingBox,
                pixelsPerDip);
        }

        private static Pen CreatePen(
            OverlayTargetViewModel target)
        {
            Pen pen =
                new(
                    target.TeamBrush,
                    target.StrokeThickness);

            if (target.IsPredicted)
            {
                pen.DashStyle =
                    new DashStyle(
                        [6.0, 3.0],
                        0.0);
            }

            pen.Freeze();

            return pen;
        }

        private void DrawLabel(
            DrawingContext drawingContext,
            OverlayTargetViewModel target,
            Rect boundingBox,
            double pixelsPerDip)
        {
            if (string.IsNullOrWhiteSpace(
                    target.Label))
            {
                return;
            }

            FormattedText formattedText =
                new(
                    target.Label,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(
                        new FontFamily(
                            "Segoe UI"),
                        FontStyles.Normal,
                        FontWeights.SemiBold,
                        FontStretches.Normal),
                    LabelFontSize,
                    Brushes.Black,
                    pixelsPerDip);

            double labelWidth =
                formattedText.Width +
                (LabelHorizontalPadding * 2.0);

            double labelHeight =
                formattedText.Height +
                (LabelVerticalPadding * 2.0);

            bool isBalloon =
                IsBalloonTarget(
                    target);

            Point labelPosition =
                CalculateLabelPosition(
                    boundingBox,
                    labelWidth,
                    labelHeight,
                    isBalloon);

            Rect labelBackground =
                new(
                    labelPosition.X,
                    labelPosition.Y,
                    labelWidth,
                    labelHeight);

            drawingContext.DrawRoundedRectangle(
                target.TeamBrush,
                pen: null,
                labelBackground,
                LabelCornerRadius,
                LabelCornerRadius);

            Point textPosition =
                new(
                    labelPosition.X +
                    LabelHorizontalPadding,

                    labelPosition.Y +
                    LabelVerticalPadding);

            drawingContext.DrawText(
                formattedText,
                textPosition);
        }

        private Point CalculateLabelPosition(
            Rect boundingBox,
            double labelWidth,
            double labelHeight,
            bool placeBelow)
        {

            double x =
                boundingBox.Left;

            double maximumX =
                Math.Max(
                    SurfaceEdgeMargin,
                    ActualWidth -
                    labelWidth -
                    SurfaceEdgeMargin);

            x =
                Math.Clamp(
                    x,
                    SurfaceEdgeMargin,
                    maximumX);

            double y =
                placeBelow
                    ? CalculateBelowPosition(
                        boundingBox,
                        labelHeight)
                    : CalculateAbovePosition(
                        boundingBox,
                        labelHeight);

            return new Point(
                x,
                y);
        }

        private double CalculateAbovePosition(
            Rect boundingBox,
            double labelHeight)
        {
            double y =
                boundingBox.Top -
                labelHeight -
                LabelBoxGap;

            if (y <
                SurfaceEdgeMargin)
            {
                y =
                    boundingBox.Bottom +
                    LabelBoxGap;
            }

            if (y + labelHeight >
                ActualHeight -
                SurfaceEdgeMargin)
            {
                y =
                    boundingBox.Top -
                    labelHeight -
                    LabelBoxGap;

                y =
                    Math.Max(
                        SurfaceEdgeMargin,
                        y);
            }

            return y;
        }

        private double CalculateBelowPosition(
            Rect boundingBox,
            double labelHeight)
        {


            double y =
                boundingBox.Bottom +
                LabelBoxGap;

            if (y + labelHeight >
                ActualHeight -
                SurfaceEdgeMargin)
            {
                y =
                    boundingBox.Top -
                    labelHeight -
                    LabelBoxGap;
            }

            if (y <
                SurfaceEdgeMargin)
            {
                y =
                    SurfaceEdgeMargin;
            }

            return y;
        }

        private static bool IsBalloonTarget(
            OverlayTargetViewModel target)
        {
            if (string.IsNullOrWhiteSpace(
                    target.Label))
            {
                return false;
            }

            return
                target.Label.Contains(
                    "BALON",
                    StringComparison.OrdinalIgnoreCase) ||
                target.Label.Contains(
                    "BALLOON",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}