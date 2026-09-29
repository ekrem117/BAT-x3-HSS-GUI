using BatX3_HSS_GUI.Application.Detection;
using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Application.Video;
using Microsoft.Extensions.Hosting;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class FrameSynchronizationHostedService : IHostedService
    {
        private readonly IVideoFrameSource _videoFrameSource;

        private readonly IDetectionFrameSource _detectionFrameSource;

        private readonly IFrameSynchronizer _frameSynchronizer;

        public FrameSynchronizationHostedService(IVideoFrameSource videoFrameSource, IDetectionFrameSource detectionFrameSource, IFrameSynchronizer frameSynchronizer)
        {
            _videoFrameSource = videoFrameSource;
            _detectionFrameSource = detectionFrameSource;
            _frameSynchronizer = frameSynchronizer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _videoFrameSource.FrameReceived += OnVideoFrameReceived;
            _detectionFrameSource.FrameReceived += OnDetectionFrameReceived;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _videoFrameSource.FrameReceived -= OnVideoFrameReceived;
            _detectionFrameSource.FrameReceived -= OnDetectionFrameReceived;
            _frameSynchronizer.Reset();

            return Task.CompletedTask;
        }

        private void OnVideoFrameReceived(object? sender, VideoFrameReceivedEventArgs eventArgs)
        {
            _frameSynchronizer.PushVideo(eventArgs.Frame);
        }

        private void OnDetectionFrameReceived(object? sender, DetectionFrameReceivedEventArgs eventArgs)
        {
            _frameSynchronizer.PushDetection(eventArgs.Frame);
        }
    }
}