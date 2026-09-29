using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Client.ViewModels.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class DiagnosticsPresenterHostedService :
        BackgroundService
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

        private readonly IChannelHealthService _channelHealthService;

        private readonly IFrameSynchronizer _frameSynchronizer;

        private readonly DiagnosticsViewModel _viewModel;

        public DiagnosticsPresenterHostedService(IChannelHealthService channelHealthService, IFrameSynchronizer frameSynchronizer, DiagnosticsViewModel viewModel)
        {
            _channelHealthService = channelHealthService;
            _frameSynchronizer = frameSynchronizer;
            _viewModel = viewModel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(RefreshInterval);

            await RefreshAsync();

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync();
            }
        }

        private async Task RefreshAsync()
        {
            IReadOnlyList<ChannelHealthSnapshot> snapshots = _channelHealthService.GetAllSnapshots();

            long matched = _frameSynchronizer.MatchedFrameCount;
            long videoOnly = _frameSynchronizer.VideoOnlyFrameCount;
            long droppedDetection = _frameSynchronizer.DroppedDetectionFrameCount;

            if (System.Windows.Application.Current?.Dispatcher is not { } dispatcher) { return; }

            await dispatcher.InvokeAsync(
                () =>
                {
                    foreach (ChannelHealthSnapshot snapshot in snapshots)
                    {
                        _viewModel.ApplyChannelSnapshot(snapshot);
                    }

                    _viewModel.MatchedFrames = matched;
                    _viewModel.VideoOnlyFrames = videoOnly;
                    _viewModel.DroppedDetectionFrames = droppedDetection;
                });
        }
    }
}