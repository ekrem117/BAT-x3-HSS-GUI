using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.Polling;
using BatX3_HSS_GUI.Domain.System;
using BatX3_HSS_GUI.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;

namespace BatX3_HSS_GUI.WebApi.Services;

public sealed class OperationStatusBroadcastService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    private readonly IParameterService _parameterService;
    private readonly ISystemRuntimeState _systemRuntimeState;
    private readonly IHubContext<TurretHub> _hubContext;
    private readonly ILogger<OperationStatusBroadcastService> _logger;

    public OperationStatusBroadcastService(
        IParameterService parameterService,
        ISystemRuntimeState systemRuntimeState,
        IHubContext<TurretHub> hubContext,
        ILogger<OperationStatusBroadcastService> logger)
    {
        _parameterService = parameterService;
        _systemRuntimeState = systemRuntimeState;
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
            IReadOnlyDictionary<string, ParameterOperationResult> results =
                await _parameterService.GetAsync(
                    ParameterPollingProfile.StatusParameters,
                    cancellationToken);

            UpdateSystemRuntimeState(results);

            var snapshot = new
            {
                Mode = GetInt(results, ParameterNames.System.Mode),
                Link = GetInt(results, ParameterNames.System.Link),
                Fps = GetDouble(results, ParameterNames.System.Fps),
                ConfidenceThreshold = GetDouble(results, ParameterNames.Vision.ConfidenceThreshold),
                DetectionCount = GetInt(results, ParameterNames.Vision.DetectionCount),
                InferenceMilliseconds = GetDouble(results, ParameterNames.Vision.InferenceMilliseconds),
                Armed = GetInt(results, ParameterNames.Weapon.Armed),
                FireMode = GetInt(results, ParameterNames.Weapon.FireMode),
                BurstCount = GetInt(results, ParameterNames.Weapon.BurstCount),
                SelectedWeapon = GetInt(results, ParameterNames.Weapon.Selected),
                Ammo = GetInt(results, ParameterNames.Weapon.Ammo),
                ShotsFired = GetInt(results, ParameterNames.Weapon.ShotsFired)
            };

            await _hubContext.Clients.All.SendAsync("ReceiveStatus", snapshot, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host kapanışı.
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Operation status polling cycle failed.");

            _systemRuntimeState.MarkUnknown();
        }
    }

    private void UpdateSystemRuntimeState(
        IReadOnlyDictionary<string, ParameterOperationResult> results)
    {
        int? modeValue = GetInt(results, ParameterNames.System.Mode);

        if (modeValue is null || !Enum.IsDefined(typeof(SystemOperatingMode), modeValue.Value))
        {
            _systemRuntimeState.MarkUnknown();
            return;
        }

        _systemRuntimeState.UpdateMode((SystemOperatingMode)modeValue.Value);
    }

    private static int? GetInt(
        IReadOnlyDictionary<string, ParameterOperationResult> results,
        string name)
    {
        if (!results.TryGetValue(name, out ParameterOperationResult result) ||
            !result.IsSuccess ||
            result.Value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(result.Value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
            when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static double? GetDouble(
        IReadOnlyDictionary<string, ParameterOperationResult> results,
        string name)
    {
        if (!results.TryGetValue(name, out ParameterOperationResult result) ||
            !result.IsSuccess ||
            result.Value is null)
        {
            return null;
        }

        try
        {
            double value = Convert.ToDouble(result.Value, CultureInfo.InvariantCulture);

            return double.IsNaN(value) || double.IsInfinity(value) ? null : value;
        }
        catch (Exception exception)
            when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }
}
