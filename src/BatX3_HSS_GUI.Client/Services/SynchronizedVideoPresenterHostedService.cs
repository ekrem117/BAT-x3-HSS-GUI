using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Client.Presentation.Video;
using BatX3_HSS_GUI.Client.ViewModels.Video;
using BatX3_HSS_GUI.Domain.Synchronization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Channels;
using System.Windows.Media.Imaging;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class SynchronizedVideoPresenterHostedService :
        BackgroundService
    {
        private readonly IFrameSynchronizer
            _frameSynchronizer;

        private readonly VideoViewModel
            _videoViewModel;

        private readonly ILogger<SynchronizedVideoPresenterHostedService>
            _logger;

        private readonly Channel<SynchronizedFrame> _frames =
            Channel.CreateBounded<SynchronizedFrame>(
                new BoundedChannelOptions(1)
                {
                    SingleReader =
                        true,

                    SingleWriter =
                        false,

                    FullMode =
                        BoundedChannelFullMode.DropOldest
                });

        public SynchronizedVideoPresenterHostedService(
            IFrameSynchronizer frameSynchronizer,
            VideoViewModel videoViewModel,
            ILogger<SynchronizedVideoPresenterHostedService> logger)
        {
            _frameSynchronizer =
                frameSynchronizer;

            _videoViewModel =
                videoViewModel;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _frameSynchronizer.FrameReady +=
                OnFrameReady;

            try
            {
                while (
                    await _frames.Reader.WaitToReadAsync(
                        stoppingToken))
                {
                    SynchronizedFrame? latestFrame =
                        null;

                    while (
                        _frames.Reader.TryRead(
                            out SynchronizedFrame? frame))
                    {
                        latestFrame =
                            frame;
                    }

                    if (latestFrame is null)
                    {
                        continue;
                    }

                    try
                    {
                        BitmapImage bitmap =
                            DecodeJpeg(
                                latestFrame);

                        IReadOnlyList<OverlayTargetViewModel> overlays =
                            VideoOverlayFactory.Create(
                                latestFrame.Detection,
                                latestFrame.Video.Width,
                                latestFrame.Video.Height);

                        await ApplyFrameAsync(
                            latestFrame,
                            bitmap,
                            overlays);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Synchronized video frame hazırlanamadı. FrameId={FrameId}",
                            latestFrame.FrameId);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal host shutdown.
            }
            finally
            {
                _frameSynchronizer.FrameReady -=
                    OnFrameReady;
            }
        }

        private void OnFrameReady(
            object? sender,
            SynchronizedFrameReadyEventArgs eventArgs)
        {
            _frames.Writer.TryWrite(
                eventArgs.Frame);
        }

        private static BitmapImage DecodeJpeg(
            SynchronizedFrame synchronizedFrame)
        {
            using MemoryStream stream =
                new(
                    synchronizedFrame.Video.JpegBytes,
                    writable: false);

            BitmapImage bitmap =
                new();

            bitmap.BeginInit();

            bitmap.CacheOption =
                BitmapCacheOption.OnLoad;

            bitmap.StreamSource =
                stream;

            bitmap.EndInit();

            bitmap.Freeze();

            return bitmap;
        }

        private async Task ApplyFrameAsync(
            SynchronizedFrame synchronizedFrame,
            BitmapImage bitmap,
            IReadOnlyList<OverlayTargetViewModel> overlays)
        {
            if (System.Windows.Application.Current?.Dispatcher
                is not { } dispatcher)
            {
                return;
            }

            await dispatcher.InvokeAsync(
                () =>
                {
                    _videoViewModel.ApplyFrame(
                        synchronizedFrame,
                        bitmap,
                        overlays);
                });
        }
    }
}