using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Input;
using Microsoft.Extensions.Logging;
using System.Windows.Input;

namespace BatX3_HSS_GUI.Client.Input
{
    public sealed class KeyboardShortcutService :
            IKeyboardShortcutService
    {
        private readonly ISettingsService
            _settingsService;

        private readonly IApplicationActionService
            _actionService;

        private readonly IManualInputCoordinator
            _manualInputCoordinator;

        private readonly ILogger<KeyboardShortcutService>
            _logger;

        private readonly object _syncRoot =
            new();

        private Dictionary<ShortcutGesture, string>
            _bindings =
                new();

        private readonly Dictionary<Key, string>
            _activeActionsByKey =
                new();

        private Action<Key, ModifierKeys>?
            _captureCompleted;

        private Action?
            _captureCanceled;

        public KeyboardShortcutService(
            ISettingsService settingsService,
            IApplicationActionService actionService,
            IManualInputCoordinator manualInputCoordinator,
            ILogger<KeyboardShortcutService> logger)
        {
            _settingsService =
                settingsService;

            _actionService =
                actionService;

            _manualInputCoordinator =
                manualInputCoordinator;

            _logger =
                logger;

            Reload();
        }

        public bool IsCaptureActive
        {
            get
            {
                lock (_syncRoot)
                {
                    return _captureCompleted is not null;
                }
            }
        }

        public void Reload()
        {
            InputSettings settings =
                _settingsService.GetInputSettings();

            Dictionary<ShortcutGesture, string> bindings =
                new();

            foreach (
                ShortcutBindingSettings shortcut
                in settings.Shortcuts)
            {
                if (!Enum.TryParse(
                        shortcut.Key,
                        ignoreCase: true,
                        out Key key))
                {
                    _logger.LogWarning(
                        "Geçersiz klavye kısayol tuşu yok sayıldı. " +
                        "Action={ActionId}, Key={Key}",
                        shortcut.ActionId,
                        shortcut.Key);

                    continue;
                }

                ModifierKeys modifiers =
                    ConvertModifiers(
                        shortcut.Modifiers);

                ShortcutGesture gesture =
                    new(
                        key,
                        modifiers);

                bindings[gesture] =
                    shortcut.ActionId;
            }

            lock (_syncRoot)
            {
                _bindings =
                    bindings;
            }

            _logger.LogInformation(
                "{Count} klavye kısayolu yüklendi.",
                bindings.Count);
        }

        public void BeginCapture(
            Action<Key, ModifierKeys> captureCompleted,
            Action? captureCanceled = null)
        {
            ArgumentNullException.ThrowIfNull(
                captureCompleted);

            lock (_syncRoot)
            {
                if (_captureCompleted is not null)
                {
                    throw new InvalidOperationException(
                        "Başka bir klavye kısayolu yakalama işlemi zaten aktif.");
                }

                _captureCompleted =
                    captureCompleted;

                _captureCanceled =
                    captureCanceled;
            }

            _logger.LogDebug(
                "Klavye kısayolu yakalama modu başlatıldı.");
        }

        public void CancelCapture()
        {
            Action? captureCanceled;

            lock (_syncRoot)
            {
                if (_captureCompleted is null)
                {
                    return;
                }

                captureCanceled =
                    _captureCanceled;

                ClearCaptureUnsafe();
            }

            _logger.LogDebug(
                "Klavye kısayolu yakalama modu iptal edildi.");

            captureCanceled?.Invoke();
        }

        public bool TryCaptureKeyDown(
            Key key,
            ModifierKeys modifiers,
            bool isRepeat)
        {
            Action<Key, ModifierKeys>? captureCompleted =
                null;

            Action? captureCanceled =
                null;

            bool shouldComplete =
                false;

            bool shouldCancel =
                false;

            lock (_syncRoot)
            {
                if (_captureCompleted is null)
                {
                    return false;
                }

                /*
                 * Capture aktifken bütün tekrar KeyDown event'leri
                 * tüketilir.
                 *
                 * Böylece basılı tutulan bir tuş birden fazla capture
                 * sonucu oluşturmaz.
                 */
                if (isRepeat)
                {
                    return true;
                }

                if (key == Key.Escape)
                {
                    captureCanceled =
                        _captureCanceled;

                    ClearCaptureUnsafe();

                    shouldCancel =
                        true;
                }
                else if (IsModifierKey(
                             key))
                {
                    /*
                     * Ctrl / Shift / Alt / Windows tek başına
                     * shortcut'ın ana tuşu değildir.
                     *
                     * Modifier event'i tüketilir ve gerçek ana tuş
                     * beklenir.
                     */
                    return true;
                }
                else if (!IsCapturableKey(
                             key))
                {
                    /*
                     * None / System / IME vb. geçersiz ana tuşlar
                     * capture'ı sonlandırmaz.
                     */
                    return true;
                }
                else
                {
                    captureCompleted =
                        _captureCompleted;

                    ClearCaptureUnsafe();

                    shouldComplete =
                        true;
                }
            }

            if (shouldCancel)
            {
                _logger.LogDebug(
                    "Klavye kısayolu yakalama işlemi Escape ile iptal edildi.");

                captureCanceled?.Invoke();

                return true;
            }

            if (shouldComplete)
            {
                ModifierKeys normalizedModifiers =
                    NormalizeModifiers(
                        modifiers);

                _logger.LogDebug(
                    "Klavye kısayolu yakalandı. " +
                    "Key={Key}, Modifiers={Modifiers}",
                    key,
                    normalizedModifiers);

                captureCompleted?.Invoke(
                    key,
                    normalizedModifiers);

                return true;
            }

            return true;
        }

        public async Task<bool> TryPressAsync(
            Key key,
            ModifierKeys modifiers,
            CancellationToken cancellationToken = default)
        {
            /*
             * Capture aktifken normal application action dispatch
             * kesinlikle yapılmaz.
             *
             * MainWindow normalde capture yolunu önce çağırır,
             * fakat bu kontrol service boundary'de de korunur.
             */
            if (IsCaptureActive)
            {
                return true;
            }

            ShortcutGesture gesture =
                new(
                    key,
                    NormalizeModifiers(
                        modifiers));

            string? actionId;

            lock (_syncRoot)
            {
                /*
                 * Aynı fiziksel tuş basılı tutuluyorsa ikinci bir
                 * action oluşturulmaz.
                 */
                if (_activeActionsByKey.ContainsKey(
                        key))
                {
                    return true;
                }

                if (!_bindings.TryGetValue(
                        gesture,
                        out actionId))
                {
                    return false;
                }

                _activeActionsByKey[key] =
                    actionId;
            }

            try
            {
                await DispatchPressAsync(
                    actionId,
                    cancellationToken);

                _logger.LogDebug(
                    "Klavye kısayolu basıldı. " +
                    "Action={ActionId}, Key={Key}, Modifiers={Modifiers}",
                    actionId,
                    key,
                    modifiers);
            }
            catch (ApplicationActionNotAllowedException exception)
            {
                /*
                 * Mode gating nedeniyle action kesin olarak
                 * gönderilmedi.
                 *
                 * ManualInputCoordinator'a eklenmiş olabilecek
                 * Keyboard ownership de temizlenir.
                 */
                if (IsManualMotionAction(
                        actionId))
                {
                    try
                    {
                        await _manualInputCoordinator.SetActiveAsync(
                            ManualInputSource.Keyboard,
                            actionId,
                            false,
                            cancellationToken);
                    }
                    catch (Exception releaseException)
                    {
                        _logger.LogWarning(
                            releaseException,
                            "Mode gating sonrasında keyboard manual " +
                            "input rollback tamamlanamadı. " +
                            "Action={ActionId}",
                            actionId);
                    }
                }

                RemoveActiveKey(
                    key,
                    actionId);

                _logger.LogDebug(
                    exception,
                    "Klavye kısayolu mevcut sistem modunda kullanılamadı. " +
                    "Action={ActionId}, Key={Key}",
                    actionId,
                    key);
            }
            catch (Exception exception)
            {
                /*
                 * Network sonucu belirsiz olabilir.
                 *
                 * Key kaydını kaldırmıyoruz.
                 * KeyUp / fail-safe release yolu korunur.
                 */
                _logger.LogError(
                    exception,
                    "Klavye kısayolu basma işlemi başarısız veya belirsiz. " +
                    "Action={ActionId}, Key={Key}",
                    actionId,
                    key);
            }

            return true;
        }

        public async Task<bool> TryReleaseAsync(
            Key key,
            CancellationToken cancellationToken = default)
        {
            string? actionId;

            lock (_syncRoot)
            {
                if (!_activeActionsByKey.TryGetValue(
                        key,
                        out actionId))
                {
                    return false;
                }
            }

            try
            {
                await DispatchReleaseAsync(
                    actionId,
                    cancellationToken);

                RemoveActiveKey(
                    key,
                    actionId);

                _logger.LogDebug(
                    "Klavye kısayolu bırakıldı. " +
                    "Action={ActionId}, Key={Key}",
                    actionId,
                    key);
            }
            catch (Exception exception)
            {
                /*
                 * Release sonucu belirsizse active key kaydı
                 * korunur.
                 *
                 * Böylece fail-safe release daha sonra tekrar
                 * denenebilir.
                 */
                _logger.LogError(
                    exception,
                    "Klavye kısayolu bırakma işlemi başarısız veya belirsiz. " +
                    "Action={ActionId}, Key={Key}",
                    actionId,
                    key);
            }

            return true;
        }

        public async Task ReleaseAllAsync(
            CancellationToken cancellationToken = default)
        {
            Key[] activeKeys;

            lock (_syncRoot)
            {
                activeKeys =
                    _activeActionsByKey
                        .Keys
                        .ToArray();
            }

            if (activeKeys.Length == 0)
            {
                return;
            }

            _logger.LogInformation(
                "{Count} aktif klavye action'ı fail-safe release ediliyor.",
                activeKeys.Length);

            foreach (
                Key key
                in activeKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await TryReleaseAsync(
                    key,
                    cancellationToken);
            }
        }

        private Task DispatchPressAsync(
            string actionId,
            CancellationToken cancellationToken)
        {
            if (IsManualMotionAction(
                    actionId))
            {
                /*
                 * Manuel hareketler doğrudan ApplicationActionService'e
                 * gitmez.
                 *
                 * Keyboard ve Gamepad aynı source-aware coordinator
                 * üzerinden birleştirilir.
                 */
                return _manualInputCoordinator.SetActiveAsync(
                    ManualInputSource.Keyboard,
                    actionId,
                    true,
                    cancellationToken);
            }

            /*
             * Weapon.Fire gibi manuel hareket dışındaki keyboard
             * action'ları mevcut application action zincirini kullanır.
             */
            return _actionService.PressAsync(
                actionId,
                cancellationToken);
        }

        private Task DispatchReleaseAsync(
            string actionId,
            CancellationToken cancellationToken)
        {
            if (IsManualMotionAction(
                    actionId))
            {
                return _manualInputCoordinator.SetActiveAsync(
                    ManualInputSource.Keyboard,
                    actionId,
                    false,
                    cancellationToken);
            }

            return _actionService.ReleaseAsync(
                actionId,
                cancellationToken);
        }

        private void RemoveActiveKey(
            Key key,
            string actionId)
        {
            lock (_syncRoot)
            {
                if (_activeActionsByKey.TryGetValue(
                        key,
                        out string? activeActionId) &&
                    string.Equals(
                        activeActionId,
                        actionId,
                        StringComparison.Ordinal))
                {
                    _activeActionsByKey.Remove(
                        key);
                }
            }
        }

        private void ClearCaptureUnsafe()
        {
            /*
             * Bu metod yalnız _syncRoot lock'u tutulurken
             * çağrılmalıdır.
             */
            _captureCompleted =
                null;

            _captureCanceled =
                null;
        }

        private static bool IsManualMotionAction(
            string actionId)
        {
            return
                string.Equals(
                    actionId,
                    ApplicationActionIds.MotionUp,
                    StringComparison.Ordinal) ||
                string.Equals(
                    actionId,
                    ApplicationActionIds.MotionDown,
                    StringComparison.Ordinal) ||
                string.Equals(
                    actionId,
                    ApplicationActionIds.MotionLeft,
                    StringComparison.Ordinal) ||
                string.Equals(
                    actionId,
                    ApplicationActionIds.MotionRight,
                    StringComparison.Ordinal);
        }

        private static bool IsModifierKey(
            Key key)
        {
            return key is
                Key.LeftCtrl or
                Key.RightCtrl or
                Key.LeftShift or
                Key.RightShift or
                Key.LeftAlt or
                Key.RightAlt or
                Key.LWin or
                Key.RWin;
        }

        private static bool IsCapturableKey(
            Key key)
        {
            return key is not
                (
                    Key.None or
                    Key.System or
                    Key.ImeProcessed or
                    Key.DeadCharProcessed
                );
        }

        private static ModifierKeys ConvertModifiers(
            ShortcutModifiers modifiers)
        {
            ModifierKeys result =
                ModifierKeys.None;

            if (modifiers.HasFlag(
                    ShortcutModifiers.Control))
            {
                result |=
                    ModifierKeys.Control;
            }

            if (modifiers.HasFlag(
                    ShortcutModifiers.Alt))
            {
                result |=
                    ModifierKeys.Alt;
            }

            if (modifiers.HasFlag(
                    ShortcutModifiers.Shift))
            {
                result |=
                    ModifierKeys.Shift;
            }

            if (modifiers.HasFlag(
                    ShortcutModifiers.Windows))
            {
                result |=
                    ModifierKeys.Windows;
            }

            return result;
        }

        private static ModifierKeys NormalizeModifiers(
            ModifierKeys modifiers)
        {
            return modifiers &
                (
                    ModifierKeys.Control |
                    ModifierKeys.Alt |
                    ModifierKeys.Shift |
                    ModifierKeys.Windows
                );
        }

        private readonly record struct ShortcutGesture(
            Key Key,
            ModifierKeys Modifiers);
    }
}