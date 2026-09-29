using BatX3_HSS_GUI.Application.Communication.Command;
using BatX3_HSS_GUI.Application.Communication.Command.Exceptions;
using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Diagnostics;
using BatX3_HSS_GUI.Domain.Communication.Command;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Command
{
    public sealed class UdpCommandService :
        ICommandService,
        IHostedService,
        IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger<UdpCommandService> _logger;
        private readonly IChannelHealthService _channelHealthService;

        private readonly ConcurrentDictionary<int, PendingCommandRequest>
            _pendingRequests = new();

        private readonly SemaphoreSlim _sendLock =
            new(1, 1);

        private readonly object _lifecycleLock =
            new();

        private UdpClient? _udpClient;

        private IPEndPoint? _remoteEndPoint;

        private CancellationTokenSource? _receiveCancellation;

        private Task? _receiveTask;

        private TimeSpan _responseTimeout;

        private int _messageId;

        private int _localPort;

        private bool _disposed;

        public UdpCommandService(
            ISettingsService settingsService,
            ILogger<UdpCommandService> logger,
            IChannelHealthService channelHealthService)
        {
            _settingsService =
                settingsService;

            _logger =
                logger;

            _channelHealthService =
                channelHealthService;
        }

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

        public int LocalPort =>
            _localPort;

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

            lock (_lifecycleLock)
            {
                ThrowIfDisposed();

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

                IPAddress localAddress =
                    IPAddress.Parse(
                        settings.LocalBindIp);

                IPAddress remoteAddress =
                    IPAddress.Parse(
                        settings.ServerIp);

                IPEndPoint localEndPoint =
                    new(
                        localAddress,
                        settings.Command.LocalPort);

                UdpClient udpClient =
                    new(localEndPoint);

                try
                {
                    IPEndPoint remoteEndPoint =
                        new(
                            remoteAddress,
                            settings.Command.RemotePort);

                    TimeSpan responseTimeout =
                        TimeSpan.FromMilliseconds(
                            settings.Command.ResponseTimeoutMs);

                    CancellationTokenSource receiveCancellation =
                        new();

                    int localPort =
                        ((IPEndPoint)udpClient.Client.LocalEndPoint!)
                        .Port;

                    /*
                     * Runtime state yalnız socket kurulumu
                     * başarılı olduktan sonra atanır.
                     */
                    _udpClient =
                        udpClient;

                    _remoteEndPoint =
                        remoteEndPoint;

                    _responseTimeout =
                        responseTimeout;

                    _localPort =
                        localPort;

                    _receiveCancellation =
                        receiveCancellation;

                    _receiveTask =
                        ReceiveLoopAsync(
                            udpClient,
                            receiveCancellation.Token);

                    TimeSpan staleAfter =
                        TimeSpan.FromMilliseconds(
                            Math.Max(
                                1000,
                                settings.Command.ResponseTimeoutMs * 4));

                    _channelHealthService.ReportStarted(
                        DiagnosticChannel.Command,
                        staleAfter);

                    _logger.LogInformation(
                        "Command UDP started. Local={LocalAddress}:{LocalPort}, Remote={RemoteAddress}:{RemotePort}",
                        localAddress,
                        _localPort,
                        remoteAddress,
                        remoteEndPoint.Port);

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
            CancellationTokenSource? receiveCancellation;
            Task? receiveTask;
            UdpClient? udpClient;

            lock (_lifecycleLock)
            {
                receiveCancellation =
                    _receiveCancellation;

                receiveTask =
                    _receiveTask;

                udpClient =
                    _udpClient;

                _receiveCancellation =
                    null;

                _receiveTask =
                    null;

                _udpClient =
                    null;

                _remoteEndPoint =
                    null;

                _localPort =
                    0;
            }

            if (udpClient is null)
            {
                _channelHealthService.ReportStopped(
                    DiagnosticChannel.Command);

                return;
            }

            if (receiveCancellation is not null)
            {
                await receiveCancellation.CancelAsync();
            }

            try
            {
                if (receiveTask is not null)
                {
                    await receiveTask.WaitAsync(
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            finally
            {
                udpClient.Dispose();

                receiveCancellation?.Dispose();

                CancelPendingRequests();

                _channelHealthService.ReportStopped(
                    DiagnosticChannel.Command);
            }

            _logger.LogInformation(
                "Command UDP stopped.");
        }

        public async Task<CommandResponse> GetAsync(
            IReadOnlyCollection<string> parameterNames,
            CancellationToken cancellationToken = default)
        {
            int messageId =
                NextMessageId();

            byte[] payload =
                CommandMessageSerializer.SerializeGet(
                    messageId,
                    parameterNames);

            return await SendRequestAsync(
                messageId,
                payload,
                CommandResponseMethod.GetResponse,
                cancellationToken);
        }

        public async Task<CommandResponse> SetAsync(
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            int messageId =
                NextMessageId();

            byte[] payload =
                CommandMessageSerializer.SerializeSet(
                    messageId,
                    parameters);

            return await SendRequestAsync(
                messageId,
                payload,
                CommandResponseMethod.SetResponse,
                cancellationToken);
        }

        private async Task<CommandResponse> SendRequestAsync(
            int messageId,
            byte[] payload,
            CommandResponseMethod expectedMethod,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            UdpClient udpClient =
                _udpClient
                ?? throw new InvalidOperationException(
                    "Command UDP service is not running.");

            IPEndPoint remoteEndPoint =
                _remoteEndPoint
                ?? throw new InvalidOperationException(
                    "Command remote endpoint is not configured.");

            PendingCommandRequest pending =
                new(expectedMethod);

            if (!_pendingRequests.TryAdd(
                    messageId,
                    pending))
            {
                throw new InvalidOperationException(
                    $"Duplicate messageID generated: {messageId}");
            }

            try
            {
                await _sendLock.WaitAsync(
                    cancellationToken);

                try
                {
                    await udpClient.SendAsync(
                        payload,
                        remoteEndPoint,
                        cancellationToken);
                }
                catch (SocketException exception)
                {
                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        exception.Message);

                    throw;
                }
                finally
                {
                    _sendLock.Release();
                }

                _logger.LogDebug(
                    "Command request sent. MessageId={MessageId}, ExpectedResponse={ExpectedResponse}",
                    messageId,
                    expectedMethod);

                try
                {
                    return await pending.Completion.Task
                        .WaitAsync(
                            _responseTimeout,
                            cancellationToken);
                }
                catch (TimeoutException)
                {
                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        $"Command timeout. MessageId={messageId}");

                    throw new CommandTimeoutException(
                        messageId,
                        _responseTimeout);
                }
            }
            finally
            {
                _pendingRequests.TryRemove(
                    messageId,
                    out _);
            }
        }

        private async Task ReceiveLoopAsync(
            UdpClient udpClient,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result;

                try
                {
                    result =
                        await udpClient.ReceiveAsync(
                            cancellationToken);
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
                        DiagnosticChannel.Command,
                        exception.Message);

                    _logger.LogError(
                        exception,
                        "Command UDP receive error.");

                    continue;
                }
                catch (Exception exception)
                {
                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        exception.Message);

                    _logger.LogError(
                        exception,
                        "Command UDP receive unexpected error.");

                    continue;
                }

                IPEndPoint? expectedRemoteEndPoint =
                    _remoteEndPoint;

                if (expectedRemoteEndPoint is not null &&
                    !result.RemoteEndPoint.Equals(
                        expectedRemoteEndPoint))
                {
                    _logger.LogWarning(
                        "Command packet ignored from unexpected endpoint {RemoteEndPoint}.",
                        result.RemoteEndPoint);

                    continue;
                }

                if (!CommandMessageParser.TryParse(
                        result.Buffer,
                        out CommandResponse? response,
                        out string? error))
                {
                    string errorMessage =
                        error
                        ?? "Command protocol parse error.";

                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        errorMessage);

                    _logger.LogWarning(
                        "Invalid command response ignored: {Error}",
                        errorMessage);

                    continue;
                }

                if (response?.MessageId is null)
                {
                    string errorMessage =
                        $"Command response without messageID. Method={response?.Method}";

                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        errorMessage);

                    _logger.LogWarning(
                        "Command response without messageID ignored. Method={Method}",
                        response?.Method);

                    continue;
                }

                int messageId =
                    response.MessageId.Value;

                if (!_pendingRequests.TryRemove(
                        messageId,
                        out PendingCommandRequest? pending))
                {
                    _logger.LogWarning(
                        "Command response has no pending request. MessageId={MessageId}",
                        messageId);

                    continue;
                }

                if (response.Method != pending.ExpectedMethod &&
                    response.Method != CommandResponseMethod.ErrorResponse)
                {
                    string errorMessage =
                        $"Unexpected response method. " +
                        $"MessageId={messageId}, " +
                        $"Expected={pending.ExpectedMethod}, " +
                        $"Received={response.Method}.";

                    _channelHealthService.ReportError(
                        DiagnosticChannel.Command,
                        errorMessage);

                    pending.Completion.TrySetException(
                        new CommandProtocolException(
                            errorMessage));

                    continue;
                }

                /*
                 * Parse edilmiş ve messageID ile eşleşmiş response,
                 * transport açısından başarılı haberleşmedir.
                 *
                 * ERRRSP içeriğinin business/protocol sonucu üst
                 * katmanda ayrıca değerlendirilebilir.
                 */
                _channelHealthService.ReportSuccess(
                    DiagnosticChannel.Command);

                pending.Completion.TrySetResult(
                    response);
            }
        }

        private int NextMessageId()
        {
            int messageId =
                Interlocked.Increment(
                    ref _messageId);

            if (messageId > 0)
            {
                return messageId;
            }

            lock (_lifecycleLock)
            {
                if (_messageId <= 0)
                {
                    _messageId =
                        1;
                }

                return _messageId;
            }
        }

        private void CancelPendingRequests()
        {
            foreach (
                KeyValuePair<int, PendingCommandRequest> request
                in _pendingRequests)
            {
                if (_pendingRequests.TryRemove(
                        request.Key,
                        out PendingCommandRequest? pending))
                {
                    pending.Completion.TrySetCanceled();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(
                _disposed,
                this);
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

            CancelPendingRequests();

            _receiveCancellation?.Dispose();

            _sendLock.Dispose();

            _channelHealthService.ReportStopped(
                DiagnosticChannel.Command);
        }
    }
}