using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Detection;
using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Domain.Detection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Detection
{
    public sealed class UdpDetectionFrameSource : IDetectionFrameSource, IHostedService, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<UdpDetectionFrameSource> _logger;
        private readonly IChannelHealthService _channelHealthService;

        private readonly object _lifecycleLock = new();

        private UdpClient? _udpClient;

        private CancellationTokenSource? _receiveCancellation;

        private Task? _receiveTask;

        private bool _disposed;

        public UdpDetectionFrameSource(ISettingsService settingsService, ILogger<UdpDetectionFrameSource> logger, IChannelHealthService channelHealthService)
        {
            _settingsService = settingsService;
            _logger = logger;
            _channelHealthService = channelHealthService;
        }

        public event EventHandler<DetectionFrameReceivedEventArgs>? FrameReceived;

        public bool IsRunning
        {
            get
            {
                lock (_lifecycleLock)
                {
                    return _udpClient is not null;
                }
            }
        }

        public int LocalPort { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            NetworkSettings settings = _settingsService.GetNetworkSettings();

            return StartWithSettingsAsync(settings, cancellationToken);
        }

        internal Task StartWithSettingsAsync(NetworkSettings settings, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(settings);

            ThrowIfDisposed();

            lock (_lifecycleLock)
            {
                if (_udpClient is not null)
                {
                    return Task.CompletedTask;
                }

                IReadOnlyList<string> validationErrors = NetworkSettingsValidator.Validate(settings);

                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
                }

                IPAddress bindAddress = IPAddress.Parse(settings.LocalBindIp);

                IPAddress expectedServerAddress = IPAddress.Parse(settings.ServerIp);

                UdpClient udpClient = new(new IPEndPoint(bindAddress, settings.Detection.ListenPort));

                try
                {
                    CancellationTokenSource receiveCancellation = new();

                    int localPort = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;

                    _udpClient = udpClient;
                    _receiveCancellation = receiveCancellation;
                    LocalPort = localPort;
                    _receiveTask = ReceiveLoopAsync(udpClient, expectedServerAddress, receiveCancellation.Token);

                    _channelHealthService.ReportStarted(DiagnosticChannel.Detection, TimeSpan.FromMilliseconds(settings.Detection.StaleTimeoutMs));

                    _logger.LogInformation("Detection UDP receiver başladı. LocalPort={LocalPort}", LocalPort);

                    return Task.CompletedTask;
                }
                catch
                {
                    udpClient.Dispose();

                    throw;
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            UdpClient? udpClient;
            CancellationTokenSource? receiveCancellation;
            Task? receiveTask;

            lock (_lifecycleLock)
            {
                udpClient = _udpClient;
                receiveCancellation = _receiveCancellation;
                receiveTask = _receiveTask;
                _udpClient = null;
                _receiveCancellation = null;
                _receiveTask = null;
                LocalPort = 0;
            }

            if (udpClient is null)
            {
                _channelHealthService.ReportStopped(DiagnosticChannel.Detection);

                return;
            }

            receiveCancellation?.Cancel();

            if (receiveTask is not null)
            {
                try
                {
                    await receiveTask.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal host shutdown.
                }
            }

            udpClient.Dispose();

            receiveCancellation?.Dispose();

            _channelHealthService.ReportStopped(DiagnosticChannel.Detection);

            _logger.LogInformation("Detection UDP receiver durduruldu.");
        }

        private async Task ReceiveLoopAsync(UdpClient udpClient, IPAddress expectedServerAddress, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult receiveResult = await udpClient.ReceiveAsync(cancellationToken);

                    if (!receiveResult.RemoteEndPoint.Address.Equals(expectedServerAddress))
                    {
                        _logger.LogDebug("Beklenmeyen adresten detection datagram alındı. Remote={Remote}", receiveResult.RemoteEndPoint);

                        continue;
                    }

                    if (!DetectionDatagramParser.TryParse(receiveResult.Buffer, out DetectionFrame? frame, out string? error))
                    {
                        string errorMessage = error ?? "Detection datagram parse error.";

                        _channelHealthService.ReportError(DiagnosticChannel.Detection, errorMessage);

                        _logger.LogWarning("Geçersiz detection datagram: {Error}", errorMessage);

                        continue;
                    }

                    if (frame is null)
                    {
                        continue;
                    }

                    _channelHealthService.ReportSuccess(DiagnosticChannel.Detection);

                    RaiseFrameReceived(frame);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException exception)
                {
                    _channelHealthService.ReportError(DiagnosticChannel.Detection, exception.Message);

                    _logger.LogWarning(exception, "Detection UDP receive hatası.");
                }
                catch (Exception exception)
                {
                    _channelHealthService.ReportError(DiagnosticChannel.Detection, exception.Message);

                    _logger.LogError(exception, "Detection receive loop beklenmeyen hata.");
                }
            }
        }

        private void RaiseFrameReceived(DetectionFrame frame)
        {
            try
            {
                FrameReceived?.Invoke(this, new DetectionFrameReceivedEventArgs(frame));
            }
            catch (Exception exception)
            {
                /*
                 * Subscriber hatası detection UDP transport
                 * health metriğine dahil edilmez.
                 */
                _logger.LogError(exception, "Detection frame subscriber hatası.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UdpDetectionFrameSource));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _receiveCancellation?.Cancel();

            _udpClient?.Dispose();

            _receiveCancellation?.Dispose();

            _channelHealthService.ReportStopped(DiagnosticChannel.Detection);
        }
    }
}