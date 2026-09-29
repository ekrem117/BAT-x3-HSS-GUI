using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.UI;
using BatX3_HSS_GUI.Domain.Parameters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace BatX3_HSS_GUI.Client.ViewModels.Parameters
{
    public partial class ParameterItemViewModel :
           ObservableObject
    {
        private readonly IParameterService
            _parameterService;

        private bool _isApplyingExternalValue;

        public ParameterItemViewModel(
            ParameterDefinition definition,
            ParameterUiDefinition uiDefinition,
            IParameterService parameterService)
        {
            Definition =
                definition;

            UiDefinition =
                uiDefinition;

            _parameterService =
                parameterService;
        }

        public ParameterDefinition Definition { get; }

        public ParameterUiDefinition UiDefinition { get; }

        public string ParameterName =>
            Definition.Name;

        public string DisplayName =>
            UiDefinition.DisplayName;

        public string? Unit =>
            Definition.Unit;

        public ParameterControlType ControlType =>
            UiDefinition.ControlType;

        public IReadOnlyList<ParameterOption> Options =>
            UiDefinition.Options;

        public double? Minimum =>
            Definition.Minimum;

        public double? Maximum =>
            Definition.Maximum;

        public bool CanRead =>
            Definition.CanRead;

        public bool CanWrite =>
            Definition.CanWrite;

        // ============================================================
        // EDIT STATE
        // ============================================================

        [ObservableProperty]
        private string _numericText =
            string.Empty;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private object? _currentValue;

        [ObservableProperty]
        private object? _pendingValue;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        public bool HasError =>
            !string.IsNullOrWhiteSpace(
                ErrorMessage);

        partial void OnNumericTextChanged(
            string value)
        {
            if (_isApplyingExternalValue)
            {
                return;
            }

            IsDirty =
                true;

            ErrorMessage =
                null;
        }

        partial void OnErrorMessageChanged(
            string? value)
        {
            OnPropertyChanged(
                nameof(HasError));
        }

        // ============================================================
        // READ
        // ============================================================

        [RelayCommand]
        private async Task ReadAsync()
        {
            if (!CanRead ||
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
                ParameterOperationResult result =
                    await _parameterService.GetAsync(
                        ParameterName);

                if (!result.IsSuccess)
                {
                    ErrorMessage =
                        $"GET başarısız: {result.Status}";

                    return;
                }

                /*
                 * Kullanıcı açıkça Oku komutuna bastığı için
                 * mevcut edit değeri server değeriyle değiştirilir.
                 */
                ApplyValue(
                    result.Value);
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

        // ============================================================
        // WRITE
        // ============================================================

        [RelayCommand]
        private async Task WriteAsync()
        {
            if (!CanWrite ||
                IsBusy)
            {
                return;
            }

            ErrorMessage =
                null;

            if (ControlType ==
                    ParameterControlType.Numeric &&
                !TryPrepareNumericValue())
            {
                return;
            }

            if (PendingValue is null)
            {
                ErrorMessage =
                    "SET için değer seçilmelidir.";

                return;
            }

            IsBusy =
                true;

            try
            {
                ParameterOperationResult result =
                    await _parameterService.SetAsync(
                        ParameterName,
                        PendingValue);

                if (!result.IsSuccess)
                {
                    ErrorMessage =
                        $"SET başarısız: {result.Status}";

                    return;
                }

                ApplyValue(
                    result.Value);
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

        // ============================================================
        // NUMERIC VALIDATION
        // ============================================================

        private bool TryPrepareNumericValue()
        {
            if (Definition.ValueType ==
                ParameterValueType.Integer)
            {
                if (!int.TryParse(
                        NumericText,
                        NumberStyles.Integer,
                        CultureInfo.CurrentCulture,
                        out int integerValue))
                {
                    ErrorMessage =
                        $"'{DisplayName}' için geçerli bir tam sayı giriniz.";

                    return false;
                }

                if (!IsWithinRange(
                        integerValue))
                {
                    SetRangeError();

                    return false;
                }

                PendingValue =
                    integerValue;

                return true;
            }

            if (Definition.ValueType ==
                ParameterValueType.FloatingPoint)
            {
                if (!double.TryParse(
                        NumericText,
                        NumberStyles.Float,
                        CultureInfo.CurrentCulture,
                        out double floatingPointValue) ||
                    double.IsNaN(
                        floatingPointValue) ||
                    double.IsInfinity(
                        floatingPointValue))
                {
                    ErrorMessage =
                        $"'{DisplayName}' için geçerli bir sayı giriniz.";

                    return false;
                }

                if (!IsWithinRange(
                        floatingPointValue))
                {
                    SetRangeError();

                    return false;
                }

                PendingValue =
                    floatingPointValue;

                return true;
            }

            return true;
        }

        private bool IsWithinRange(
            double value)
        {
            if (Minimum.HasValue &&
                value < Minimum.Value)
            {
                return false;
            }

            if (Maximum.HasValue &&
                value > Maximum.Value)
            {
                return false;
            }

            return true;
        }

        private void SetRangeError()
        {
            if (Minimum.HasValue &&
                Maximum.HasValue)
            {
                ErrorMessage =
                    $"'{DisplayName}' değeri " +
                    $"{Minimum.Value.ToString(CultureInfo.CurrentCulture)} ile " +
                    $"{Maximum.Value.ToString(CultureInfo.CurrentCulture)} " +
                    $"arasında olmalıdır.";

                return;
            }

            if (Minimum.HasValue)
            {
                ErrorMessage =
                    $"'{DisplayName}' değeri en az " +
                    $"{Minimum.Value.ToString(CultureInfo.CurrentCulture)} olmalıdır.";

                return;
            }

            if (Maximum.HasValue)
            {
                ErrorMessage =
                    $"'{DisplayName}' değeri en fazla " +
                    $"{Maximum.Value.ToString(CultureInfo.CurrentCulture)} olmalıdır.";

                return;
            }

            ErrorMessage =
                $"'{DisplayName}' için geçersiz değer.";
        }

        // ============================================================
        // VALUE SYNCHRONIZATION
        // ============================================================

        private void ApplyValue(
            object? value)
        {
            CurrentValue =
                value;

            PendingValue =
                value;

            if (ControlType ==
                    ParameterControlType.Numeric &&
                value is not null)
            {
                SetNumericTextFromExternalValue(
                    value);
            }

            IsDirty =
                false;
        }

        private void SetNumericTextFromExternalValue(
            object value)
        {
            _isApplyingExternalValue =
                true;

            try
            {
                NumericText =
                    Convert.ToString(
                        value,
                        CultureInfo.CurrentCulture)
                    ?? string.Empty;
            }
            finally
            {
                _isApplyingExternalValue =
                    false;
            }
        }

        // ============================================================
        // INITIAL BATCH READ
        // ============================================================

        public void ApplyInitialReadResult(
            ParameterOperationResult result)
        {
            if (!string.Equals(
                    result.Name,
                    ParameterName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"İlk okuma sonucu yanlış parameter için geldi. " +
                    $"Beklenen: {ParameterName}, Gelen: {result.Name}",
                    nameof(result));
            }

            if (!result.IsSuccess)
            {
                ErrorMessage =
                    $"İlk okuma başarısız: {result.Status}";

                return;
            }

            ErrorMessage =
                null;

            /*
             * İlk batched GET devam ederken kullanıcı field üzerinde
             * edit yapmışsa kullanıcının yazdığı değer korunur.
             *
             * Server'ın son değeri CurrentValue üzerinde yine tutulur.
             */
            if (IsDirty)
            {
                CurrentValue =
                    result.Value;

                return;
            }

            ApplyValue(
                result.Value);
        }

        public void MarkInitialReadUnavailable()
        {
            ErrorMessage =
                "Parametre değeri ilk okumada alınamadı.";
        }

        // ============================================================
        // BACKGROUND POLLING
        // ============================================================

        public void ApplyPollingResult(
            ParameterOperationResult result)
        {
            if (!string.Equals(
                    result.Name,
                    ParameterName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Polling sonucu yanlış parameter için geldi. " +
                    $"Beklenen: {ParameterName}, Gelen: {result.Name}",
                    nameof(result));
            }

            if (!result.IsSuccess)
            {
                ErrorMessage =
                    $"Polling GET başarısız: {result.Status}";

                return;
            }

            ErrorMessage =
                null;

            /*
             * Polling yalnızca gerçek server değerini günceller.
             * Kullanıcının edit ettiği NumericText/PendingValue alanına
             * dokunmaz.
             */
            CurrentValue =
                result.Value;
        }
    }
}