using BatX3_HSS_GUI.Application.Gamepad;
using System.Runtime.CompilerServices;
using Windows.Gaming.Input;
using WindowsGamepad =
    Windows.Gaming.Input.Gamepad;

namespace BatX3_HSS_GUI.Client.Input.Gamepad
{
    public sealed class WindowsGamepadDeviceProvider :
        IGamepadDeviceProvider,
        IDisposable
    {
        /*
         * Profile sırası önemlidir.
         *
         * Önce fiziksel olarak doğrulanmış özel profiller,
         * en son generic compatibility fallback değerlendirilir.
         */
        private static readonly
            IRawGamepadProfile[] RawProfiles =
            [
                Gp307RawGamepadProfile.Instance,
                Gp307CompatibleRawGamepadProfile.Instance
            ];

        private readonly object _syncRoot =
            new();

        private readonly Dictionary<
            string,
            ControllerEntry> _controllers =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
            WindowsGamepad,
            string> _idsByGamepad =
                new(
                    ReferenceEqualityComparer.Instance);

        private readonly Dictionary<
            RawGameController,
            string> _idsByRawController =
                new(
                    ReferenceEqualityComparer.Instance);

        private bool _disposed;

        public WindowsGamepadDeviceProvider()
        {
            WindowsGamepad.GamepadAdded +=
                OnGamepadAdded;

            WindowsGamepad.GamepadRemoved +=
                OnGamepadRemoved;

            RawGameController.RawGameControllerAdded +=
                OnRawGameControllerAdded;

            RawGameController.RawGameControllerRemoved +=
                OnRawGameControllerRemoved;

            /*
             * Önce modern Windows.Gaming.Input.Gamepad
             * cihazlarını kaydediyoruz.
             */
            foreach (
                WindowsGamepad gamepad
                in WindowsGamepad.Gamepads)
            {
                AddGamepad(
                    gamepad);
            }

            /*
             * Daha sonra raw-only / legacy controller'ları
             * değerlendiriyoruz.
             *
             * Aynı fiziksel cihaz modern Gamepad olarak da
             * kullanılabiliyorsa AddRawController modern yolu
             * tercih edecektir.
             */
            foreach (
                RawGameController rawController
                in RawGameController.RawGameControllers)
            {
                AddRawController(
                    rawController);
            }
        }

        public IReadOnlyList<GamepadDeviceInfo>
            GetConnectedDevices()
        {
            ThrowIfDisposed();

            lock (_syncRoot)
            {
                return _controllers
                    .Values
                    .Select(
                        entry =>
                            entry.Info)
                    .OrderBy(
                        info =>
                            info.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        info =>
                            info.Id,
                        StringComparer.Ordinal)
                    .ToArray();
            }
        }

        public bool TryRead(
            string deviceId,
            out GamepadReadingSnapshot? snapshot)
        {
            ThrowIfDisposed();

            ArgumentException.ThrowIfNullOrWhiteSpace(
                deviceId);

            ControllerEntry? entry;

            lock (_syncRoot)
            {
                if (!_controllers.TryGetValue(
                        deviceId,
                        out entry))
                {
                    snapshot =
                        null;

                    return false;
                }
            }

            /*
             * Modern Gamepad yolu her zaman önceliklidir.
             */
            if (entry.Gamepad is not null)
            {
                return TryReadGamepad(
                    entry.Gamepad,
                    out snapshot);
            }

            /*
             * Legacy / RawGameController yolu.
             */
            if (entry.RawController is not null &&
                entry.RawProfile is not null)
            {
                return entry.RawProfile.TryRead(
                    entry.RawController,
                    out snapshot);
            }

            snapshot =
                null;

            return false;
        }

        private static bool TryReadGamepad(
            WindowsGamepad gamepad,
            out GamepadReadingSnapshot? snapshot)
        {
            ArgumentNullException.ThrowIfNull(
                gamepad);

            GamepadReading reading =
                gamepad.GetCurrentReading();

            snapshot =
                new GamepadReadingSnapshot
                {
                    LeftStickX =
                        reading.LeftThumbstickX,

                    LeftStickY =
                        reading.LeftThumbstickY,

                    LeftTrigger =
                        reading.LeftTrigger,

                    RightTrigger =
                        reading.RightTrigger,

                    PressedControls =
                        ConvertButtons(
                            reading.Buttons)
                };

            return true;
        }

        private void OnGamepadAdded(
            object? sender,
            WindowsGamepad gamepad)
        {
            if (_disposed)
            {
                return;
            }

            AddGamepad(
                gamepad);
        }

        private void OnGamepadRemoved(
            object? sender,
            WindowsGamepad gamepad)
        {
            if (_disposed)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (!_idsByGamepad.TryGetValue(
                        gamepad,
                        out string? id))
                {
                    return;
                }

                _idsByGamepad.Remove(
                    gamepad);

                /*
                 * Aynı ID daha sonra başka bir entry tarafından
                 * devralınmış olabilir.
                 *
                 * Yalnız gerçekten bu Gamepad'e ait entry'yi siliyoruz.
                 */
                if (_controllers.TryGetValue(
                        id,
                        out ControllerEntry? entry) &&
                    ReferenceEquals(
                        entry.Gamepad,
                        gamepad))
                {
                    _controllers.Remove(
                        id);
                }
            }
        }

        private void OnRawGameControllerAdded(
            object? sender,
            RawGameController rawController)
        {
            if (_disposed)
            {
                return;
            }

            AddRawController(
                rawController);
        }

        private void OnRawGameControllerRemoved(
            object? sender,
            RawGameController rawController)
        {
            if (_disposed)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (!_idsByRawController.TryGetValue(
                        rawController,
                        out string? id))
                {
                    return;
                }

                _idsByRawController.Remove(
                    rawController);

                /*
                 * Aynı fiziksel cihaz daha sonra modern Gamepad
                 * entry'sine yükseltilmiş olabilir.
                 *
                 * Bu durumda RawRemoved modern entry'yi silmemelidir.
                 */
                if (_controllers.TryGetValue(
                        id,
                        out ControllerEntry? entry) &&
                    ReferenceEquals(
                        entry.RawController,
                        rawController))
                {
                    _controllers.Remove(
                        id);
                }
            }
        }

        private void AddGamepad(
            WindowsGamepad gamepad)
        {
            ArgumentNullException.ThrowIfNull(
                gamepad);

            GamepadDeviceInfo info =
                CreateGamepadDeviceInfo(
                    gamepad);

            lock (_syncRoot)
            {
                if (_idsByGamepad.ContainsKey(
                        gamepad))
                {
                    return;
                }

                /*
                 * Aynı fiziksel cihaz daha önce raw event üzerinden
                 * compatibility profile ile eklenmiş olabilir.
                 *
                 * Modern Gamepad her zaman daha güvenilir olduğundan
                 * raw entry yerine modern entry'yi tercih ediyoruz.
                 */
                if (_controllers.TryGetValue(
                        info.Id,
                        out ControllerEntry? existingEntry) &&
                    existingEntry.RawController is not null)
                {
                    _idsByRawController.Remove(
                        existingEntry.RawController);
                }

                _controllers[info.Id] =
                    new ControllerEntry(
                        info,
                        gamepad,
                        null,
                        null);

                _idsByGamepad[gamepad] =
                    info.Id;
            }
        }

        private void AddRawController(
            RawGameController rawController)
        {
            ArgumentNullException.ThrowIfNull(
                rawController);

            /*
             * Aynı fiziksel controller Windows tarafından standart
             * Gamepad olarak da kullanılabiliyorsa modern yolu tercih
             * ediyoruz.
             *
             * Sadece event'in daha sonra gelmesini beklemek yerine
             * burada doğrudan AddGamepad çağırıyoruz. Böylece event
             * ordering'e bağımlılık azalır.
             */
            try
            {
                WindowsGamepad? standardGamepad =
                    WindowsGamepad.FromGameController(
                        rawController);

                if (standardGamepad is not null)
                {
                    AddGamepad(
                        standardGamepad);

                    return;
                }
            }
            catch
            {
                /*
                 * Standard Gamepad dönüşümünün başarısız olması,
                 * raw controller'ın kullanılamayacağı anlamına gelmez.
                 *
                 * Raw profile değerlendirmesine devam edilir.
                 */
            }

            IRawGamepadProfile? profile =
                FindRawProfile(
                    rawController);

            if (profile is null)
            {
                /*
                 * Minimum GP-307 compatibility yapısını dahi
                 * karşılamayan controller'dan arbitrary input
                 * üretmiyoruz.
                 */
                return;
            }

            GamepadDeviceInfo info =
                CreateRawDeviceInfo(
                    rawController,
                    profile);

            lock (_syncRoot)
            {
                if (_idsByRawController.ContainsKey(
                        rawController))
                {
                    return;
                }

                /*
                 * Aynı fiziksel ID ile modern Gamepad zaten varsa
                 * raw kayıt modern kaydı ezemez.
                 */
                if (_controllers.TryGetValue(
                        info.Id,
                        out ControllerEntry? existingEntry) &&
                    existingEntry.Gamepad is not null)
                {
                    return;
                }

                _controllers[info.Id] =
                    new ControllerEntry(
                        info,
                        null,
                        rawController,
                        profile);

                _idsByRawController[rawController] =
                    info.Id;
            }
        }

        private static IRawGamepadProfile?
            FindRawProfile(
                RawGameController rawController)
        {
            ArgumentNullException.ThrowIfNull(
                rawController);

            foreach (
                IRawGamepadProfile profile
                in RawProfiles)
            {
                /*
                 * Gp307RawGamepadProfile listede önce olduğu için
                 * doğrulanmış GP-307 hiçbir zaman generic fallback'e
                 * düşmez.
                 */
                if (profile.IsMatch(
                        rawController))
                {
                    return profile;
                }
            }

            return null;
        }

        private static GamepadDeviceInfo
            CreateRawDeviceInfo(
                RawGameController rawController,
                IRawGamepadProfile profile)
        {
            ArgumentNullException.ThrowIfNull(
                rawController);

            ArgumentNullException.ThrowIfNull(
                profile);

            string id =
                string.IsNullOrWhiteSpace(
                    rawController.NonRoamableId)
                    ? CreateRawFallbackId(
                        rawController)
                    : rawController.NonRoamableId;

            string displayName =
                CreateRawDisplayName(
                    rawController,
                    profile);

            return new GamepadDeviceInfo(
                id,
                displayName);
        }

        private static GamepadDeviceInfo
            CreateGamepadDeviceInfo(
                WindowsGamepad gamepad)
        {
            ArgumentNullException.ThrowIfNull(
                gamepad);

            try
            {
                RawGameController? rawController =
                    RawGameController.FromGameController(
                        gamepad);

                if (rawController is not null)
                {
                    string displayName =
                        string.IsNullOrWhiteSpace(
                            rawController.DisplayName)
                            ? "Gamepad"
                            : rawController.DisplayName.Trim();

                    string id =
                        string.IsNullOrWhiteSpace(
                            rawController.NonRoamableId)
                            ? CreateGamepadFallbackId(
                                gamepad)
                            : rawController.NonRoamableId;

                    return new GamepadDeviceInfo(
                        id,
                        displayName);
                }
            }
            catch
            {
                /*
                 * Metadata alınamaması standart Gamepad input'unun
                 * kullanılmasını engellememelidir.
                 */
            }

            return new GamepadDeviceInfo(
                CreateGamepadFallbackId(
                    gamepad),
                "Gamepad");
        }

        private static string CreateRawDisplayName(
            RawGameController rawController,
            IRawGamepadProfile profile)
        {
            /*
             * Fiziksel olarak doğrulanmış GP-307 için
             * açık ürün adını gösteriyoruz.
             */
            if (ReferenceEquals(
                    profile,
                    Gp307RawGamepadProfile.Instance))
            {
                return profile.DisplayName;
            }

            string controllerName =
                string.IsNullOrWhiteSpace(
                    rawController.DisplayName)
                    ? "USB Gamepad"
                    : rawController.DisplayName.Trim();

            /*
             * Bilinmeyen VID/PID fakat GP-307 fiziksel layout
             * koşullarını karşılayan cihaz kullanıcıdan gizlenmez.
             */
            if (ReferenceEquals(
                    profile,
                    Gp307CompatibleRawGamepadProfile.Instance))
            {
                return
                    $"{controllerName} " +
                    "(GP-307 Uyumluluk Modu)";
            }

            return profile.DisplayName;
        }

        private static string CreateGamepadFallbackId(
            WindowsGamepad gamepad)
        {
            return
                $"gamepad-runtime-" +
                $"{RuntimeHelpers.GetHashCode(gamepad):X8}";
        }

        private static string CreateRawFallbackId(
            RawGameController rawController)
        {
            return
                $"raw-" +
                $"{rawController.HardwareVendorId:X4}-" +
                $"{rawController.HardwareProductId:X4}-" +
                $"{RuntimeHelpers.GetHashCode(rawController):X8}";
        }

        private static IReadOnlySet<GamepadControl>
            ConvertButtons(
                GamepadButtons buttons)
        {
            HashSet<GamepadControl> result =
                new();

            AddIfPressed(
                buttons,
                GamepadButtons.A,
                GamepadControl.A,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.B,
                GamepadControl.B,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.X,
                GamepadControl.X,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.Y,
                GamepadControl.Y,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.LeftShoulder,
                GamepadControl.LeftShoulder,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.RightShoulder,
                GamepadControl.RightShoulder,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.Menu,
                GamepadControl.Menu,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.View,
                GamepadControl.View,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.DPadUp,
                GamepadControl.DPadUp,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.DPadDown,
                GamepadControl.DPadDown,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.DPadLeft,
                GamepadControl.DPadLeft,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.DPadRight,
                GamepadControl.DPadRight,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.LeftThumbstick,
                GamepadControl.LeftThumbstick,
                result);

            AddIfPressed(
                buttons,
                GamepadButtons.RightThumbstick,
                GamepadControl.RightThumbstick,
                result);

            return result;
        }

        private static void AddIfPressed(
            GamepadButtons buttons,
            GamepadButtons flag,
            GamepadControl control,
            ISet<GamepadControl> result)
        {
            if (buttons.HasFlag(
                    flag))
            {
                result.Add(
                    control);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(
                        WindowsGamepadDeviceProvider));
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

            WindowsGamepad.GamepadAdded -=
                OnGamepadAdded;

            WindowsGamepad.GamepadRemoved -=
                OnGamepadRemoved;

            RawGameController.RawGameControllerAdded -=
                OnRawGameControllerAdded;

            RawGameController.RawGameControllerRemoved -=
                OnRawGameControllerRemoved;

            lock (_syncRoot)
            {
                _controllers.Clear();

                _idsByGamepad.Clear();

                _idsByRawController.Clear();
            }
        }

        private sealed class ControllerEntry
        {
            public ControllerEntry(
                GamepadDeviceInfo info,
                WindowsGamepad? gamepad,
                RawGameController? rawController,
                IRawGamepadProfile? rawProfile)
            {
                Info =
                    info;

                Gamepad =
                    gamepad;

                RawController =
                    rawController;

                RawProfile =
                    rawProfile;
            }

            public GamepadDeviceInfo Info
            {
                get;
            }

            public WindowsGamepad? Gamepad
            {
                get;
            }

            public RawGameController? RawController
            {
                get;
            }

            public IRawGamepadProfile? RawProfile
            {
                get;
            }
        }
    }
}