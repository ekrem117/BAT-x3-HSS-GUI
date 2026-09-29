using BatX3_HSS_GUI.Application.Gamepad;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Settings
{
    public partial class
       GamepadBindingEditorItemViewModel :
           ObservableObject
    {
        public GamepadBindingEditorItemViewModel(
            string actionId,
            string displayName,
            GamepadControl selectedControl,
            IReadOnlyList<GamepadControlOptionViewModel>
                availableControls)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                actionId);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                displayName);

            ArgumentNullException.ThrowIfNull(
                availableControls);

            ActionId =
                actionId;

            DisplayName =
                displayName;

            AvailableControls =
                availableControls;

            _selectedControl =
                selectedControl;
        }

        public string ActionId
        {
            get;
        }

        public string DisplayName
        {
            get;
        }

        public IReadOnlyList<
            GamepadControlOptionViewModel>
            AvailableControls
        {
            get;
        }

        [ObservableProperty]
        private GamepadControl
            _selectedControl;

        [ObservableProperty]
        [NotifyPropertyChangedFor(
            nameof(CaptureButtonText))]
        [NotifyPropertyChangedFor(
            nameof(CanEditManually))]
        private bool
            _isCapturing;

        public string CaptureButtonText =>
            IsCapturing
                ? "İptal"
                : "Değiştir";

        public bool CanEditManually =>
            !IsCapturing;

        public GamepadBindingSettings ToSettings()
        {
            return new GamepadBindingSettings
            {
                ActionId =
                    ActionId,

                Control =
                    SelectedControl
            };
        }
    }
}