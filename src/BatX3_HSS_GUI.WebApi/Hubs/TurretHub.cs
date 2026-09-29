using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Application.OperatingModes;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.System;
using Microsoft.AspNetCore.SignalR;

namespace BatX3_HSS_GUI.WebApi.Hubs;

public class TurretHub : Hub
{
    private readonly ISystemModeService _systemModeService;
    private readonly IParameterService _parameterService;
    private readonly ISystemRuntimeState _systemRuntimeState;
    private readonly IApplicationActionService _applicationActionService;
    private readonly IGamepadAnalogMotionController _analogMotionController;

    public TurretHub(
        ISystemModeService systemModeService,
        IParameterService parameterService,
        ISystemRuntimeState systemRuntimeState,
        IApplicationActionService applicationActionService,
        IGamepadAnalogMotionController analogMotionController)
    {
        _systemModeService = systemModeService;
        _parameterService = parameterService;
        _systemRuntimeState = systemRuntimeState;
        _applicationActionService = applicationActionService;
        _analogMotionController = analogMotionController;
    }

    public async Task SetMode(string mode)
    {
        if (!Enum.TryParse(mode, ignoreCase: true, out SystemOperatingMode targetMode) ||
            !Enum.IsDefined(targetMode))
        {
            throw new HubException($"Geçersiz mod: '{mode}'.");
        }

        try
        {
            await _systemModeService.SetModeAsync(targetMode);
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task SetArmed(bool armed)
    {
        if (armed)
        {
            SystemOperatingMode? mode = _systemRuntimeState.CurrentMode;

            if (mode is not (SystemOperatingMode.Manual or SystemOperatingMode.Auto))
            {
                throw new HubException(
                    "Ateş yetkisi yalnızca MANUEL veya OTOMATİK modda açılabilir.");
            }
        }

        try
        {
            ParameterOperationResult result = await _parameterService.SetAsync(
                ParameterNames.Weapon.Armed,
                armed ? 1 : 0);

            if (!result.IsSuccess)
            {
                throw new HubException(
                    $"Ateş yetkisi değiştirilemedi. Status: {result.Status}");
            }
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task Fire()
    {
        try
        {
            await _applicationActionService.PressAsync(ApplicationActionIds.WeaponFire);
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task SetConfidenceThreshold(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0 || value > 1.0)
        {
            throw new HubException(
                "Tespit güven eşiği 0 ile 1 arasında geçerli bir sayı olmalıdır.");
        }

        try
        {
            ParameterOperationResult result = await _parameterService.SetAsync(
                ParameterNames.Vision.ConfidenceThreshold,
                value);

            if (!result.IsSuccess)
            {
                throw new HubException(
                    $"Tespit güven eşiği değiştirilemedi. Status: {result.Status}");
            }
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task SetWeaponConfiguration(int fireMode, int burstCount, int weaponSelected)
    {
        Dictionary<string, object> parameters = new(StringComparer.Ordinal)
        {
            [ParameterNames.Weapon.FireMode] = fireMode,
            [ParameterNames.Weapon.BurstCount] = burstCount,
            [ParameterNames.Weapon.Selected] = weaponSelected
        };

        try
        {
            IReadOnlyDictionary<string, ParameterOperationResult> results =
                await _parameterService.SetAsync(parameters);

            List<string> failures = [];

            foreach (KeyValuePair<string, object> requested in parameters)
            {
                if (!results.TryGetValue(requested.Key, out ParameterOperationResult result) ||
                    !result.IsSuccess)
                {
                    failures.Add(requested.Key);
                }
            }

            if (failures.Count > 0)
            {
                throw new HubException(
                    $"Silah ayarları uygulanamadı: {string.Join(", ", failures)}");
            }
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public async Task<bool> SetMotion(double pan, double tilt)
    {
        try
        {
            return await _analogMotionController.ApplyAsync(
                new GamepadAnalogState(IsReady: true, Pan: pan, Tilt: tilt));
        }
        catch (Exception exception) when (exception is not HubException)
        {
            throw new HubException(exception.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _analogMotionController.ResetAsync();
        }
        catch
        {
            // Fail-safe best-effort; bağlantı zaten kapanıyor.
        }

        await base.OnDisconnectedAsync(exception);
    }
}