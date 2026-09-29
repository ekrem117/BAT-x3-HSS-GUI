using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Configuration.Input;
using BatX3_HSS_GUI.Client.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Windows;

namespace BatX3_HSS_GUI.Client.ViewModels.Settings
{
    public partial class ShortcutEditorItemViewModel :
        ObservableObject
    {
        public ShortcutEditorItemViewModel(
            ApplicationActionDefinition action,
            ShortcutBindingSettings? binding,
            IReadOnlyList<string> availableKeys)
        {
            ActionId =
                action.Id;

            DisplayName =
                action.DisplayName;

            AvailableKeys =
                availableKeys;

            if (binding is not null)
            {
                SelectedKey =
                    binding.Key;

                UseControl =
                    binding.Modifiers.HasFlag(
                        ShortcutModifiers.Control);

                UseAlt =
                    binding.Modifiers.HasFlag(
                        ShortcutModifiers.Alt);

                UseShift =
                    binding.Modifiers.HasFlag(
                        ShortcutModifiers.Shift);

                UseWindows =
                    binding.Modifiers.HasFlag(
                        ShortcutModifiers.Windows);
            }
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        /*
         * Eski ComboBox UI kaldırılmış olsa da mevcut test/API
         * uyumluluğu için katalog şimdilik korunuyor.
         */
        public IReadOnlyList<string> AvailableKeys { get; }

        public string ShortDisplayName =>
            ActionId switch
            {
                ApplicationActionIds.MotionUp =>
                    "Yukarı",

                ApplicationActionIds.MotionDown =>
                    "Aşağı",

                ApplicationActionIds.MotionLeft =>
                    "Sol",

                ApplicationActionIds.MotionRight =>
                    "Sağ",

                ApplicationActionIds.WeaponFire =>
                    "Ateş",

                _ =>
                    DisplayName
            };

        public string ShortcutText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                        SelectedKey))
                {
                    return "Atanmamış";
                }

                List<string> parts =
                    new();

                if (UseControl)
                {
                    parts.Add(
                        "Ctrl");
                }

                if (UseAlt)
                {
                    parts.Add(
                        "Alt");
                }

                if (UseShift)
                {
                    parts.Add(
                        "Shift");
                }

                if (UseWindows)
                {
                    parts.Add(
                        "Win");
                }

                parts.Add(
                    SelectedKey);

                return string.Join(
                    " + ",
                    parts);
            }
        }

        public string ShortcutDisplayText =>
            IsCapturing
                ? "Bir tuşa basın..."
                : ShortcutText;

        public string CaptureButtonText =>
            IsCapturing
                ? "İptal"
                : "Değiştir";

        [ObservableProperty]
        private string? _selectedKey;

        [ObservableProperty]
        private bool _useControl;

        [ObservableProperty]
        private bool _useAlt;

        [ObservableProperty]
        private bool _useShift;

        [ObservableProperty]
        private bool _useWindows;

        [ObservableProperty]
        private bool _isCapturing;

        partial void OnSelectedKeyChanged(
            string? value)
        {
            NotifyShortcutDisplayChanged();
        }

        partial void OnUseControlChanged(
            bool value)
        {
            NotifyShortcutDisplayChanged();
        }

        partial void OnUseAltChanged(
            bool value)
        {
            NotifyShortcutDisplayChanged();
        }

        partial void OnUseShiftChanged(
            bool value)
        {
            NotifyShortcutDisplayChanged();
        }

        partial void OnUseWindowsChanged(
            bool value)
        {
            NotifyShortcutDisplayChanged();
        }

        partial void OnIsCapturingChanged(
            bool value)
        {
            OnPropertyChanged(
                nameof(ShortcutDisplayText));

            OnPropertyChanged(
                nameof(CaptureButtonText));
        }

        public void ApplyCapturedGesture(
            Key key,
            ModifierKeys modifiers)
        {
            SelectedKey =
                key.ToString();

            UseControl =
                modifiers.HasFlag(
                    ModifierKeys.Control);

            UseAlt =
                modifiers.HasFlag(
                    ModifierKeys.Alt);

            UseShift =
                modifiers.HasFlag(
                    ModifierKeys.Shift);

            UseWindows =
                modifiers.HasFlag(
                    ModifierKeys.Windows);

            NotifyShortcutDisplayChanged();
        }

        public ShortcutBindingSettings? ToSettings()
        {
            if (string.IsNullOrWhiteSpace(
                    SelectedKey))
            {
                return null;
            }

            ShortcutModifiers modifiers =
                ShortcutModifiers.None;

            if (UseControl)
            {
                modifiers |=
                    ShortcutModifiers.Control;
            }

            if (UseAlt)
            {
                modifiers |=
                    ShortcutModifiers.Alt;
            }

            if (UseShift)
            {
                modifiers |=
                    ShortcutModifiers.Shift;
            }

            if (UseWindows)
            {
                modifiers |=
                    ShortcutModifiers.Windows;
            }

            return new ShortcutBindingSettings
            {
                ActionId =
                    ActionId,

                Key =
                    SelectedKey,

                Modifiers =
                    modifiers
            };
        }

        private void NotifyShortcutDisplayChanged()
        {
            OnPropertyChanged(
                nameof(ShortcutText));

            OnPropertyChanged(
                nameof(ShortcutDisplayText));
        }
    }
}