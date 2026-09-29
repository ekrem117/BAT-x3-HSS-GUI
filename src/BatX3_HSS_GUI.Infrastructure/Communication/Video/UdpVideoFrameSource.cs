using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Application.Video;
using BatX3_HSS_GUI.Domain.Video;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Video
{
    public sealed class UdpVideoFrameSource :
        IVideoFrameSource,
        IHostedService,
        IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<UdpVideoFrameSource> _logger;
        private readonly IChannelHealthService _channelHealthService;

        private readonly object _lifecycleLock =
            new();

        private UdpClient? _udpClient;

        private CancellationTokenSource? _receiveCancellation;

        private Task? _receiveTask;

        private bool _disposed;

        public UdpVideoFrameSource(
            ISettingsService settingsService,
            ILogger<UdpVideoFrameSource> logger,
            IChannelHealthService channelHealthService)
        {
            _settingsService =
                settingsService;

            _logger =
                logger;

            _channelHealthService =
                channelHealthService;
        }

        public event EventHandler<VideoFrameReceivedEventArgs>?
            FrameReceived;

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

        public Task StartAsync(
            CancellationToken cancellationToken)
        {
            NetworkSettings settings =
                _settingsService.GetNetworkSettings();

            return StartWithSettingsAsync(
                settings,
                cancellationToken);
        }

        internal Task StartWithSettingsAsync(
            NetworkSettings settings,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                settings);

            ThrowIfDisposed();

            lock (_lifecycleLock)
            {
                if (_udpClient is not null)
                {
                    return Task.CompletedTask;
                }

                IReadOnlyList<string> validationErrors =
                    NetworkSettingsValidator.Validate(
                        settings);

                if (validationErrors.Count > 0)
                {
                    throw new InvalidOperationException(
                        string.Join(
                            Environment.NewLine,
                            validationErrors));
                }

                IPAddress bindAddress =
                    IPAddress.Parse(
                        settings.LocalBindIp);

                IPAddress serverAddress =
                    IPAddress.Parse(
                        settings.ServerIp);

                UdpClient udpClient =
                    new(
                        new IPEndPoint(
                            bindAddress,
                            settings.Video.ListenPort));

                try
                {
                    udpClient.Client.ReceiveBufferSize =
                        settings.Video.ReceiveBufferBytes;

                    CancellationTokenSource receiveCancellation =
                        new();

                    int localPort =
                        ((IPEndPoint)udpClient.Client.LocalEndPoint!)
                        .Port;

                    _udpClient =
                        udpClient;

                    _receiveCancellation =
                        receiveCancellation;

                    LocalPort =
                        localPort;

                    _receiveTask =
                        ReceiveLoopAsync(
                            udpClient,
                            serverAddress,
                            receiveCancellation.Token);

                    _channelHealthService.ReportStarted(
                        DiagnosticChannel.Video,
                        TimeSpan.FromMilliseconds(500));

                    _logger.LogInformation(
                        "Video UDP receiver başladı. LocalPort={LocalPort}, ReceiveBuffer={ReceiveBuffer}",
                        LocalPort,
                        udpClient.Client.ReceiveBufferSize);

                    return Task.CompletedTask;
                }
                catch
                {
                    udpClient.Dispose();

                    throw;
                }
            }
        }

        public async Task StopAsync(
            CancellationToken cancellationToken)
        {
            UdpClient? udpClient;
            CancellationTokenSource? receiveCancellation;
            Task? receiveTask;

            lock (_lifecycleLock)
            {
                udpClient =
                    _udpClient;

                receiveCancellation =
                    _receiveCancellation;

                receiveTask =
                    _receiveTask;

                _udpClient =
                    null;

                _receiveCancellation =
                    null;

                _receiveTask =
                    null;

                LocalPort =
                    0;
            }

            if (udpClient is null)
            {
                _channelHealthService.ReportStopped(
                    DiagnosticChannel.Video);

                return;
            }

            receiveCancellation?.Cancel();

            if (receiveTask is not null)
            {
                try
                {
                    await receiveTask.WaitAsync(
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal host shutdown.
                }
            }

            udpClient.Dispose();

            receiveCancellation?.Dispose();

            _channelHealthService.ReportStopped(
                DiagnosticChannel.Video);

            _logger.LogInformation(
                "Video UDP receiver durduruldu.");
        }

        private async Task ReceiveLoopAsync(
            UdpClient udpClient,
            IPAddress expectedServerAddress,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult receiveResult =
                        await udpClient.ReceiveAsync(
                            cancellationToken);

                    if (!receiveResult
                            .RemoteEndPoint
                            .Address
                            .Equals(expectedServerAddress))
                    {
                        _logger.LogDebug(
                            "Beklenmeyen adresten video datagram alındı. Remote={Remote}",
                            receiveResult.RemoteEndPoint);

                        continue;
                    }

                    if (!VideoDatagramParser.TryParse(
                            receiveResult.Buffer,
                            out VideoFrame? frame,
                            out string? error))
                    {
                        string errorMessage =
                            error
                            ?? "Video datagram parse error.";

                        _channelHealthService.ReportError(
                            DiagnosticChannel.Video,
                            errorMessage);

                        _logger.LogWarning(
                            "Geçersiz video datagram: {Error}",
                            errorMessage);

                        continue;
                    }

                    if (frame is null)
                    {
                        continue;
                    }

                    /*
                     * Transport health burada raporlanır.
                     *
                     * Subscriber/render performansı video UDP health
                     * metriğinin parçası değildir.
                     */
                    _channelHealthService.ReportSuccess(
                        DiagnosticChannel.Video);

                    RaiseFrameReceived(
                        frame);
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
                    _channelHealthService.ReportError(
                        DiagnosticChannel.Video,
                        exception.Message);

                    _logger.LogWarning(
                        exception,
                        "Video UDP receive hatası.");
                }
                catch (Exception exception)
                {
                    _channelHealthService.ReportError(
                        DiagnosticChannel.Video,
                        exception.Message);

                    _logger.LogError(
                        exception,
                        "Video receive loop beklenmeyen hata.");
                }
            }
        }

        private void RaiseFrameReceived(
            VideoFrame frame)
        {
            try
            {
                FrameReceived?.Invoke(
                    this,
                    new VideoFrameReceivedEventArgs(
                        frame));
            }
            catch (Exception exception)
            {
                /*
                 * Subscriber hatası UDP transport hatası değildir.
                 */
                _logger.LogError(
                    exception,
                    "Video frame subscriber hatası.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(UdpVideoFrameSource));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed =
                true;

            _receiveCancellation?.Cancel();

            _udpClient?.Dispose();

            _receiveCancellation?.Dispose();

            _channelHealthService.ReportStopped(
                DiagnosticChannel.Video);
        }
    }
}