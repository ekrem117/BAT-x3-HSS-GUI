using BatX3_HSS_GUI.Application.Detection;
using BatX3_HSS_GUI.Client.ViewModels.Detection;
using BatX3_HSS_GUI.Domain.Detection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class DetectionPresenterHostedService : BackgroundService
    {
        private readonly IDetectionFrameSource _detectionFrameSource;

        private readonly DetectionViewModel _detectionViewModel;

        private readonly ILogger<DetectionPresenterHostedService> _logger;

        private readonly Channel<DetectionFrame> _frames = Channel.CreateBounded<DetectionFrame>(
                new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });

        public DetectionPresenterHostedService(IDetectionFrameSource detectionFrameSource, DetectionViewModel detectionViewModel, ILogger<DetectionPresenterHostedService> logger)
        {
            _detectionFrameSource = detectionFrameSource;

            _detectionViewModel = detectionViewModel;

            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _detectionFrameSource.FrameReceived += OnFrameReceived;

            try
            {
                while (await _frames.Reader.WaitToReadAsync(stoppingToken))
                {
                    DetectionFrame? latestFrame = null;

                    while (_frames.Reader.TryRead(out DetectionFrame? frame))
                    {
                        latestFrame = frame;
                    }

                    if (latestFrame is null)
                    {
                        continue;
                    }

                    await ApplyFrameAsync(latestFrame);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Detection presenter beklenmeyen hata.");
            }
            finally
            {
                _detectionFrameSource.FrameReceived -= OnFrameReceived;
            }
        }

        private void OnFrameReceived(object? sender, DetectionFrameReceivedEventArgs eventArgs)
        {
            _frames.Writer.TryWrite(eventArgs.Frame);
        }

        private async Task ApplyFrameAsync(DetectionFrame frame)
        {
            if (System.Windows.Application.Current?.Dispatcher is not { } dispatcher) { return; }

            await dispatcher.InvokeAsync(() => { _detectionViewModel.ApplyFrame(frame); });
        }
    }
}