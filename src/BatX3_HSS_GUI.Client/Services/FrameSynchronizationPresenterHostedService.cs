using BatX3_HSS_GUI.Application.Synchronization;
using BatX3_HSS_GUI.Client.ViewModels.Synchronization;
using BatX3_HSS_GUI.Domain.Synchronization;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

namespace BatX3_HSS_GUI.Client.Services
{
    public sealed class FrameSynchronizationPresenterHostedService : BackgroundService
    {
        private readonly IFrameSynchronizer _frameSynchronizer;

        private readonly FrameSynchronizationViewModel _viewModel;

        private readonly Channel<SynchronizedFrame> _frames = Channel.CreateBounded<SynchronizedFrame>(
                new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });

        public FrameSynchronizationPresenterHostedService(IFrameSynchronizer frameSynchronizer, FrameSynchronizationViewModel viewModel)
        {
            _frameSynchronizer = frameSynchronizer;
            _viewModel = viewModel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _frameSynchronizer.FrameReady += OnFrameReady;

            try
            {
                while (await _frames.Reader.WaitToReadAsync(stoppingToken))
                {
                    SynchronizedFrame? latest = null;

                    while (_frames.Reader.TryRead(out SynchronizedFrame? frame))
                    {
                        latest = frame;
                    }

                    if (latest is null)
                    {
                        continue;
                    }

                    await ApplyAsync(latest);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            finally
            {
                _frameSynchronizer.FrameReady -= OnFrameReady;
            }
        }

        private void OnFrameReady(object? sender, SynchronizedFrameReadyEventArgs eventArgs)
        {
            _frames.Writer.TryWrite(eventArgs.Frame);
        }

        private async Task ApplyAsync(SynchronizedFrame frame)
        {
            if (System.Windows.Application.Current?.Dispatcher is not { } dispatcher) { return; }

            await dispatcher.InvokeAsync(() => { _viewModel.ApplyFrame(frame); });
        }
    }
}