using BatX3_HSS_GUI.Application.Gamepad;
using BatX3_HSS_GUI.Client.Input;
using BatX3_HSS_GUI.Client.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace BatX3_HSS_GUI.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow :
    Window
{
    private readonly IKeyboardShortcutService
        _keyboardShortcutService;

    private readonly GamepadApplicationActivityState
        _gamepadApplicationActivityState;

    private readonly GamepadControlCaptureService
        _gamepadControlCaptureService;

    private bool _closeReleaseInProgress;
    private bool _closeReleaseCompleted;

    public MainWindow(
    MainWindowViewModel viewModel,
    IKeyboardShortcutService keyboardShortcutService,
    GamepadApplicationActivityState gamepadApplicationActivityState,
    GamepadControlCaptureService gamepadControlCaptureService)
    {
        InitializeComponent();

        DataContext =
            viewModel;

        _keyboardShortcutService =
            keyboardShortcutService;

        _gamepadApplicationActivityState =
            gamepadApplicationActivityState;

        _gamepadControlCaptureService =
            gamepadControlCaptureService;
    }

    protected override void OnActivated(
    EventArgs e)
    {
        base.OnActivated(
            e);

        _gamepadApplicationActivityState
            .SetActive(
                true);
    }

    protected override void OnSourceInitialized(
        EventArgs e)
    {
        base.OnSourceInitialized(
            e);

        FitInitialSizeToWorkArea();
        CenterInitialWindow();
    }

    protected override async void OnPreviewKeyDown(
        KeyEventArgs e)
    {
        base.OnPreviewKeyDown(
            e);

        Key actualKey =
            GetActualKey(
                e);

        /*
         * Shortcut capture normal application shortcut processing'den
         * önce çalışmalıdır.
         *
         * Böylece örneğin Weapon.Fire için atanmış F tuşu yeniden
         * atanırken gerçek fire action'ına ulaşmaz.
         */
        if (_keyboardShortcutService.IsCaptureActive)
        {
            bool captureHandled =
                _keyboardShortcutService.TryCaptureKeyDown(
                    actualKey,
                    Keyboard.Modifiers,
                    e.IsRepeat);

            if (captureHandled)
            {
                e.Handled =
                    true;
            }

            return;
        }

        if (Keyboard.FocusedElement is
            TextBoxBase or
            ComboBox)
        {
            return;
        }

        if (e.IsRepeat)
        {
            return;
        }

        bool handled =
            await _keyboardShortcutService.TryPressAsync(
                actualKey,
                Keyboard.Modifiers);

        if (handled)
        {
            e.Handled =
                true;
        }
    }

    protected override async void OnPreviewKeyUp(
        KeyEventArgs e)
    {
        base.OnPreviewKeyUp(
            e);

        if (_keyboardShortcutService.IsCaptureActive)
        {
            e.Handled =
                true;

            return;
        }

        Key actualKey =
            GetActualKey(
                e);

        bool handled =
            await _keyboardShortcutService.TryReleaseAsync(
                actualKey);

        if (handled)
        {
            e.Handled =
                true;
        }
    }

    protected override async void OnDeactivated(
        EventArgs e)
    {
        base.OnDeactivated(
            e);

        _gamepadApplicationActivityState
            .SetActive(
                false);

        _gamepadControlCaptureService
            .Cancel();

        if (_closeReleaseInProgress ||
            _closeReleaseCompleted)
        {
            return;
        }

        await _keyboardShortcutService
            .ReleaseAllAsync();
    }

    protected override void OnClosing(
        CancelEventArgs e)
    {
        _keyboardShortcutService.CancelCapture();

        if (_closeReleaseCompleted)
        {
            base.OnClosing(
                e);

            return;
        }

        e.Cancel =
            true;

        if (!_closeReleaseInProgress)
        {
            _closeReleaseInProgress =
                true;

            _ =
                CompleteCloseAsync();
        }

        base.OnClosing(
            e);
    }

    private async Task CompleteCloseAsync()
    {
        try
        {
            _gamepadApplicationActivityState.SetActive(false);

            _gamepadControlCaptureService.Cancel();

            await _keyboardShortcutService.ReleaseAllAsync();
        }
        catch
        {
            /*
             * Uygulama kapanışı release hatası nedeniyle
             * sonsuza kadar engellenmemelidir.
             */
        }
        finally
        {
            _closeReleaseCompleted =
                true;

            _closeReleaseInProgress =
                false;

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(
                    Close));
        }
    }

    private void FitInitialSizeToWorkArea()
    {
        Rect workArea =
            SystemParameters.WorkArea;

        double maximumInitialWidth =
            workArea.Width * 0.92;

        double maximumInitialHeight =
            workArea.Height * 0.90;

        MinWidth =
            Math.Min(
                MinWidth,
                maximumInitialWidth);

        MinHeight =
            Math.Min(
                MinHeight,
                maximumInitialHeight);

        Width =
            Math.Min(
                Width,
                maximumInitialWidth);

        Height =
            Math.Min(
                Height,
                maximumInitialHeight);
    }

    private void CenterInitialWindow()
    {
        Rect workArea =
            SystemParameters.WorkArea;

        Left =
            workArea.Left +
            ((workArea.Width - Width) / 2.0);

        Top =
            workArea.Top +
            ((workArea.Height - Height) / 2.0);
    }

    private static Key GetActualKey(
        KeyEventArgs e)
    {
        return e.Key == Key.System
            ? e.SystemKey
            : e.Key;
    }
}