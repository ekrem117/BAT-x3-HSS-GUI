using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Application.Video;
using BatX3_HSS_GUI.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BatX3_HSS_GUI.WebApi.Services;

public class VideoFrameBroadcastService : IHostedService
{
    private readonly IVideoFrameSource _videoFrameSource;
    private readonly IFrameSynchronizer _frameSynchronizer;
    private readonly IHubContext<TurretHub> _hubContext;
    private readonly ILogger<VideoFrameBroadcastService> _logger;

    public VideoFrameBroadcastService(
        IVideoFrameSource videoFrameSource,
        IFrameSynchronizer frameSynchronizer,
        IHubContext<TurretHub> hubContext,
        ILogger<VideoFrameBroadcastService> logger)
    {
        _videoFrameSource = videoFrameSource;
        _frameSynchronizer = frameSynchronizer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _videoFrameSource.FrameReceived += OnFrameReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _videoFrameSource.FrameReceived -= OnFrameReceived;
        return Task.CompletedTask;
    }

    private async void OnFrameReceived(object? sender, VideoFrameReceivedEventArgs e)
    {
        try
        {
            _frameSynchronizer.PushVideo(e.Frame);

            await _hubContext.Clients.All.SendAsync(
                "ReceiveVideoFrame",
                e.Frame.FrameId,
                e.Frame.Width,
                e.Frame.Height,
                e.Frame.JpegBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Video frame broadcast error.");
        }
    }
}
