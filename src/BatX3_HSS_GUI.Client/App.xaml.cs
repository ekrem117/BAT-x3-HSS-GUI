using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Client.ViewModels;
using BatX3_HSS_GUI.Client.Input;
using BatX3_HSS_GUI.Infrastructure.Configuration;
using BatX3_HSS_GUI.Infrastructure;
using BatX3_HSS_GUI.Application;
using BatX3_HSS_GUI.Application.Configuration.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using BatX3_HSS_GUI.Client.ViewModels.Parameters;
using BatX3_HSS_GUI.Client.Services;
using BatX3_HSS_GUI.Client.ViewModels.Video;
using BatX3_HSS_GUI.Client.ViewModels.Detection;
using BatX3_HSS_GUI.Client.ViewModels.Synchronization;
using BatX3_HSS_GUI.Client.ViewModels.Diagnostics;
using BatX3_HSS_GUI.Client.ViewModels.Operation;
using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Client.Input.Gamepad;

namespace BatX3_HSS_GUI.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App :
    System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(
            e);

        HostApplicationBuilder builder =
            Host.CreateApplicationBuilder();

        ConfigureServices(
            builder.Services,
            builder.Configuration);

        _host =
            builder.Build();

        await _host.StartAsync();

        MainWindow mainWindow =
            _host.Services.GetRequiredService<MainWindow>();

        mainWindow.Show();
    }

    protected override async void OnExit(
        ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        base.OnExit(
            e);
    }

    private static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        NetworkSettings defaultNetworkSettings =
            configuration
                .GetSection(
                    NetworkSettings.SectionName)
                .Get<NetworkSettings>()
            ?? new NetworkSettings();

        InputSettings defaultInputSettings = new();

        defaultInputSettings.Gamepad.Bindings.Clear();

        configuration
            .GetSection(
                InputSettings.SectionName)
            .Bind(
                defaultInputSettings);

        IReadOnlyList<string> validationErrors =
            NetworkSettingsValidator.Validate(
                defaultNetworkSettings);

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"appsettings.json network configuration geçersiz:" +
                $"{Environment.NewLine}" +
                string.Join(
                    Environment.NewLine,
                    validationErrors));
        }

        GamepadSettingsValidator
            gamepadSettingsValidator =
                new();

        IReadOnlyList<string>
            defaultGamepadValidationErrors =
                gamepadSettingsValidator.Validate(
                    defaultInputSettings.Gamepad);

        if (defaultGamepadValidationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"appsettings.json gamepad configuration geçersiz:" +
                $"{Environment.NewLine}" +
                string.Join(
                    Environment.NewLine,
                    defaultGamepadValidationErrors));
        }

        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        string userSettingsFilePath =
            Path.Combine(
                localApplicationData,
                "BatX3_HSS_GUI",
                "Client",
                "appsettings.user.json");

        JsonSettingsService settingsService =
            new(
                defaultNetworkSettings,
                defaultInputSettings,
                userSettingsFilePath);

        InputSettings effectiveInputSettings =
            settingsService.GetInputSettings();

        IReadOnlyList<string>
            effectiveGamepadValidationErrors =
                gamepadSettingsValidator.Validate(
                    effectiveInputSettings.Gamepad);

        if (effectiveGamepadValidationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Kullanıcı gamepad configuration geçersiz:" +
                $"{Environment.NewLine}" +
                string.Join(
                    Environment.NewLine,
                    effectiveGamepadValidationErrors));
        }

        GamepadSettingsStore gamepadSettingsStore = new(effectiveInputSettings.Gamepad);

        services.AddApplication();

        services.AddInfrastructure();

        services.AddHostedService<
            GamepadInputHostedService>();

        services.AddHostedService<
            ParameterPollingHostedService>();

        services.AddHostedService<
            SynchronizedVideoPresenterHostedService>();

        services.AddHostedService<
            FrameSynchronizationHostedService>();

        services.AddHostedService<
            DiagnosticsPresenterHostedService>();

        services.AddHostedService<
            GamepadInputHostedService>();

        services.AddHostedService<
            GamepadStatusPresenterHostedService>();

        services.AddSingleton<
            GamepadApplicationActivityState>();

        services.AddSingleton<
            IGamepadSettingsStore>(
                gamepadSettingsStore);

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddSingleton<
            GamepadControlEdgeProcessor>();

        services.AddSingleton<
            GamepadControlCaptureService>();

        services.AddSingleton<
            GamepadBindingResolver>();

        services.AddSingleton<
            IGamepadActionDispatcher,
            GamepadActionDispatcher>();

        services.AddSingleton<
            IGamepadDeviceProvider,
            WindowsGamepadDeviceProvider>();

        services.AddSingleton<
            GamepadDeviceSelectionState>();

        services.AddSingleton<
            GamepadAnalogAxisProcessor>();

        services.AddSingleton<
            IGamepadAnalogMotionController,
            GamepadAnalogMotionController>();

        services.AddSingleton<
            GamepadRuntimeState>();

        services.AddSingleton<
            GamepadInputRuntime>();

        services.AddSingleton<
            DiagnosticsViewModel>();

        services.AddSingleton<
            FrameSynchronizationViewModel>();

        services.AddSingleton<
            IKeyboardShortcutService,
            KeyboardShortcutService>();

        services.AddSingleton<
            DetectionViewModel>();

        services.AddSingleton<
            VideoViewModel>();

        services.AddSingleton<
            OperationViewModel>();

        services.AddSingleton<
            ParametersViewModel>();

        services.AddSingleton<
            SettingsViewModel>();

        services.AddSingleton<ISettingsService>(settingsService);

        services.AddSingleton<
            MainWindowViewModel>();

        services.AddSingleton<
            MainWindow>();
    }
}