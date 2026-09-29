using BatX3_HSS_GUI.Application.Actions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BatX3_HSS_GUI.Client.ViewModels.Actions
{
    public partial class ApplicationActionViewModel :
           ObservableObject
    {
        private readonly IApplicationActionService _actionService;

        public ApplicationActionViewModel(
            ApplicationActionDefinition definition,
            IApplicationActionService actionService)
        {
            Definition =
                definition;

            _actionService =
                actionService;
        }

        public ApplicationActionDefinition Definition { get; }

        public string Id =>
            Definition.Id;

        public string DisplayName =>
            Definition.DisplayName;

        public bool IsMomentary =>
            Definition.InteractionType ==
            ApplicationActionInteractionType.Momentary;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        [RelayCommand]
        private async Task ExecuteAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy =
                true;

            ErrorMessage =
                null;

            try
            {
                await _actionService.ExecuteAsync(
                    Id);
            }
            catch (Exception exception)
            {
                ErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        [RelayCommand]
        private async Task PressAsync()
        {
            if (!IsMomentary ||
                IsBusy)
            {
                return;
            }

            IsBusy =
                true;

            ErrorMessage =
                null;

            try
            {
                await _actionService.PressAsync(
                    Id);
            }
            catch (Exception exception)
            {
                ErrorMessage =
                    exception.Message;
            }
            finally
            {
                IsBusy =
                    false;
            }
        }

        [RelayCommand]
        private async Task ReleaseAsync()
        {
            if (!IsMomentary)
            {
                return;
            }

            try
            {
                await _actionService.ReleaseAsync(
                    Id);
            }
            catch (Exception exception)
            {
                ErrorMessage =
                    exception.Message;
            }
        }
    }
}