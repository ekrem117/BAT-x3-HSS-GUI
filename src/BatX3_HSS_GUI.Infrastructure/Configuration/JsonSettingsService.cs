using BatX3_HSS_GUI.Application.Configuration;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Application.Gamepad;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BatX3_HSS_GUI.Infrastructure.Configuration
{
    public sealed class JsonSettingsService :
           ISettingsService
    {
        private static readonly JsonSerializerOptions
            SerializerOptions =
                CreateSerializerOptions();

        private readonly NetworkSettings
            _defaultNetworkSettings;

        private readonly InputSettings
            _defaultInputSettings;

        private readonly string
            _userSettingsFilePath;

        public JsonSettingsService(
            NetworkSettings defaultNetworkSettings,
            InputSettings defaultInputSettings,
            string userSettingsFilePath)
        {
            ArgumentNullException.ThrowIfNull(
                defaultNetworkSettings);

            ArgumentNullException.ThrowIfNull(
                defaultInputSettings);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                userSettingsFilePath);

            _defaultNetworkSettings =
                Clone(
                    defaultNetworkSettings);

            _defaultInputSettings =
                Clone(
                    defaultInputSettings);

            _userSettingsFilePath =
                userSettingsFilePath;
        }

        public NetworkSettings GetNetworkSettings()
        {
            UserSettingsReadResult readResult =
                ReadUserSettings();

            return readResult.Root?.Network is not null
                ? Clone(
                    readResult.Root.Network)
                : Clone(
                    _defaultNetworkSettings);
        }

        public InputSettings GetInputSettings()
        {
            UserSettingsReadResult readResult =
                ReadUserSettings();

            if (readResult.Root?.Input is null)
            {
                return Clone(
                    _defaultInputSettings);
            }

            InputSettings result =
                Clone(
                    readResult.Root.Input);

            /*
             * Backward compatibility:
             *
             * Eski appsettings.user.json dosyalarında Input.Gamepad
             * bulunmayabilir.
             *
             * Property initializer nedeniyle deserialize edilmiş result
             * içinde bir GamepadSettings nesnesi oluşabilir. Bu yüzden
             * yalnız null kontrolü yeterli değildir; JSON property
             * presence ayrıca takip edilir.
             */
            if (!readResult.HasInputGamepad ||
                result.Gamepad is null)
            {
                result.Gamepad =
                    Clone(
                        _defaultInputSettings.Gamepad);
            }

            /*
             * Aynı prensibi Shortcuts için de uyguluyoruz.
             *
             * Property hiç yoksa default kullanılır.
             * Property açıkça [] ise kullanıcının boş shortcut seçimi
             * korunur.
             */
            if (!readResult.HasInputShortcuts ||
                result.Shortcuts is null)
            {
                result.Shortcuts =
                    Clone(
                        _defaultInputSettings.Shortcuts);
            }

            return result;
        }

        public VideoOverlaySettings
            GetVideoOverlaySettings()
        {
            UserSettingsReadResult readResult =
                ReadUserSettings();

            if (readResult.Root?.VideoOverlay is null)
            {
                return new VideoOverlaySettings();
            }

            return Clone(
                readResult.Root.VideoOverlay);
        }

        public NetworkSettings
            GetDefaultNetworkSettings()
        {
            return Clone(
                _defaultNetworkSettings);
        }

        public InputSettings
            GetDefaultInputSettings()
        {
            return Clone(
                _defaultInputSettings);
        }

        public async Task SaveSettingsAsync(
            NetworkSettings networkSettings,
            InputSettings inputSettings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                networkSettings);

            ArgumentNullException.ThrowIfNull(
                inputSettings);

            IReadOnlyList<string> networkErrors =
                NetworkSettingsValidator.Validate(
                    networkSettings);

            if (networkErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        networkErrors));
            }

            if (inputSettings.Gamepad is null)
            {
                throw new InvalidOperationException(
                    "Gamepad ayarları null olamaz.");
            }

            GamepadSettingsValidator
                gamepadSettingsValidator =
                    new();

            IReadOnlyList<string> gamepadErrors =
                gamepadSettingsValidator.Validate(
                    inputSettings.Gamepad);

            if (gamepadErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Join(
                        Environment.NewLine,
                        gamepadErrors));
            }

            string? directory =
                Path.GetDirectoryName(
                    _userSettingsFilePath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            /*
             * VideoOverlay UI tarafından yönetilmiyor.
             *
             * Bu nedenle Network / Input kaydedilirken mevcut
             * appsettings.user.json içindeki VideoOverlay bölümü
             * aynen korunmalıdır.
             */
            UserSettingsReadResult existingReadResult =
                ReadUserSettings();

            VideoOverlaySettings? existingVideoOverlay =
                existingReadResult.Root?.VideoOverlay is not null
                    ? Clone(
                        existingReadResult.Root.VideoOverlay)
                    : null;

            UserSettingsRoot root =
                new()
                {
                    Network =
                        Clone(
                            networkSettings),

                    Input =
                        Clone(
                            inputSettings),

                    VideoOverlay =
                        existingVideoOverlay
                };

            string json =
                JsonSerializer.Serialize(
                    root,
                    SerializerOptions);

            string temporaryFilePath =
                $"{_userSettingsFilePath}.tmp";

            try
            {
                await File.WriteAllTextAsync(
                    temporaryFilePath,
                    json,
                    cancellationToken);

                File.Move(
                    temporaryFilePath,
                    _userSettingsFilePath,
                    overwrite: true);
            }
            finally
            {
                if (File.Exists(
                        temporaryFilePath))
                {
                    File.Delete(
                        temporaryFilePath);
                }
            }
        }

        private UserSettingsReadResult
            ReadUserSettings()
        {
            if (!File.Exists(
                    _userSettingsFilePath))
            {
                return UserSettingsReadResult.Empty;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        _userSettingsFilePath);

                UserSettingsRoot? root =
                    JsonSerializer.Deserialize<UserSettingsRoot>(
                        json,
                        SerializerOptions);

                bool hasInputShortcuts =
                    false;

                bool hasInputGamepad =
                    false;

                using JsonDocument document =
                    JsonDocument.Parse(
                        json);

                if (document.RootElement.ValueKind ==
                        JsonValueKind.Object &&
                    TryGetPropertyIgnoreCase(
                        document.RootElement,
                        "Input",
                        out JsonElement inputElement) &&
                    inputElement.ValueKind ==
                        JsonValueKind.Object)
                {
                    hasInputShortcuts =
                        TryGetPropertyIgnoreCase(
                            inputElement,
                            "Shortcuts",
                            out _);

                    hasInputGamepad =
                        TryGetPropertyIgnoreCase(
                            inputElement,
                            "Gamepad",
                            out _);
                }

                return new UserSettingsReadResult(
                    root,
                    hasInputShortcuts,
                    hasInputGamepad);
            }
            catch (JsonException)
            {
                return UserSettingsReadResult.Empty;
            }
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                value =
                    default;

                return false;
            }

            foreach (
                JsonProperty property
                in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value =
                        property.Value;

                    return true;
                }
            }

            value =
                default;

            return false;
        }

        private static T Clone<T>(
            T source)
        {
            string json =
                JsonSerializer.Serialize(
                    source,
                    SerializerOptions);

            return JsonSerializer.Deserialize<T>(
                       json,
                       SerializerOptions)
                   ?? throw new InvalidOperationException(
                       $"Settings clone başarısız: {typeof(T).Name}");
        }

        private static JsonSerializerOptions
            CreateSerializerOptions()
        {
            JsonSerializerOptions options =
                new()
                {
                    WriteIndented =
                        true,

                    PropertyNameCaseInsensitive =
                        true
                };

            options.Converters.Add(
                new JsonStringEnumConverter());

            return options;
        }

        private sealed class UserSettingsRoot
        {
            public NetworkSettings? Network
            {
                get;
                set;
            }

            public InputSettings? Input
            {
                get;
                set;
            }

            public VideoOverlaySettings? VideoOverlay
            {
                get;
                set;
            }
        }

        private sealed record UserSettingsReadResult(
            UserSettingsRoot? Root,
            bool HasInputShortcuts,
            bool HasInputGamepad)
        {
            public static UserSettingsReadResult Empty
            {
                get;
            } =
                new(
                    null,
                    false,
                    false);
        }
    }
}