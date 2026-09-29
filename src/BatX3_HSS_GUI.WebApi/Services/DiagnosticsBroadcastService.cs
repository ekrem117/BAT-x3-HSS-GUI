using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BatX3_HSS_GUI.WebApi.Services;

public sealed class DiagnosticsBroadcastService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(1000);

    private readonly IChannelHealthService _channelHealthService;
    private readonly IFrameSynchronizer _frameSynchronizer;
    private readonly ISettingsService _settingsService;
    private readonly IHubContext<TurretHub> _hubContext;
    private readonly ILogger<DiagnosticsBroadcastService> _logger;

    public DiagnosticsBroadcastService(
        IChannelHealthService channelHealthService,
        IFrameSynchronizer frameSynchronizer,
        ISettingsService settingsService,
        IHubContext<TurretHub> hubContext,
        ILogger<DiagnosticsBroadcastService> logger)
    {
        _channelHealthService = channelHealthService;
        _frameSynchronizer = frameSynchronizer;
        _settingsService = settingsService;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollingInterval);

        do
        {
            await PollOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            NetworkSettings settings = _settingsService.GetNetworkSettings();

            var channels = _channelHealthService.GetAllSnapshots()
                .Select(snapshot => new
                {
                    Channel = snapshot.Channel.ToString(),
                    DisplayName = GetDisplayName(snapshot.Channel),
                    State = snapshot.State.ToString(),
                    IsRunning = snapshot.IsRunning,
                    Endpoint = GetEndpointText(snapshot.Channel, settings),
                    LastActivityMs = snapshot.LastSuccessAge?.TotalMilliseconds,
                    SuccessCount = snapshot.SuccessCount,
                    ErrorCount = snapshot.ErrorCount,
                    LastError = snapshot.LastError
                })
                .ToArray();

            var snapshotPayload = new
            {
                Channels = channels,
                MatchedFrames = _frameSynchronizer.MatchedFrameCount,
                VideoOnlyFrames = _frameSynchronizer.VideoOnlyFrameCount,
                DroppedDetectionFrames = _frameSynchronizer.DroppedDetectionFrameCount
            };

            await _hubContext.Clients.All.SendAsync("ReceiveDiagnostics", snapshotPayload, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host kapanışı.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Diagnostics broadcast cycle failed.");
        }
    }

    private static string GetDisplayName(DiagnosticChannel channel)
    {
        return channel switch
        {
            DiagnosticChannel.Command => "Komut",
            DiagnosticChannel.Video => "Video",
            DiagnosticChannel.Detection => "Tespit",
            _ => channel.ToString()
        };
    }

    private static string GetEndpointText(DiagnosticChannel channel, NetworkSettings settings)
    {
        return channel switch
        {
            DiagnosticChannel.Command => $"{settings.ServerIp}:{settings.Command.RemotePort}",
            DiagnosticChannel.Video => $"{settings.LocalBindIp}:{settings.Video.ListenPort}",
            DiagnosticChannel.Detection => $"{settings.LocalBindIp}:{settings.Detection.ListenPort}",
            _ => "—"
        };
    }
}
