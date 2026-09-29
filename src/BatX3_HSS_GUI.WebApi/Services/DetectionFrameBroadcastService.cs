using BatX3_HSS_GUI.Application.Detection;
using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BatX3_HSS_GUI.WebApi.Services;

public class DetectionFrameBroadcastService : IHostedService
{
    private readonly IDetectionFrameSource _detectionFrameSource;
    private readonly IFrameSynchronizer _frameSynchronizer;
    private readonly IHubContext<TurretHub> _hubContext;
    private readonly ILogger<DetectionFrameBroadcastService> _logger;

    public DetectionFrameBroadcastService(
        IDetectionFrameSource detectionFrameSource,
        IFrameSynchronizer frameSynchronizer,
        IHubContext<TurretHub> hubContext,
        ILogger<DetectionFrameBroadcastService> logger)
    {
        _detectionFrameSource = detectionFrameSource;
        _frameSynchronizer = frameSynchronizer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _detectionFrameSource.FrameReceived += OnFrameReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _detectionFrameSource.FrameReceived -= OnFrameReceived;
        return Task.CompletedTask;
    }

    private async void OnFrameReceived(object? sender, DetectionFrameReceivedEventArgs e)
    {
        try
        {
            _frameSynchronizer.PushDetection(e.Frame);

            await _hubContext.Clients.All.SendAsync("ReceiveDetectionFrame", e.Frame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detection frame broadcast error.");
        }
    }
}
