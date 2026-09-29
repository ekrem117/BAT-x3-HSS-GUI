using BatX3_HSS_GUI.Application.Communication.Command;
using BatX3_HSS_GUI.Domain.Communication.Command;
using BatX3_HSS_GUI.Domain.Parameters;
using System.Globalization;

namespace BatX3_HSS_GUI.Application.Parameters
{
    public sealed class ParameterService : IParameterService
    {
        private readonly ICommandService _commandService;
        private readonly IParameterCatalog _catalog;

        public ParameterService(
            ICommandService commandService,
            IParameterCatalog catalog)
        {
            _commandService = commandService;
            _catalog = catalog;
        }

        public async Task<ParameterOperationResult> GetAsync(
            string parameterName,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, ParameterOperationResult> results =
                await GetAsync(
                    new[] { parameterName },
                    cancellationToken);

            return results[parameterName];
        }

        public async Task<
            IReadOnlyDictionary<string, ParameterOperationResult>>
            GetAsync(
                IReadOnlyCollection<string> parameterNames,
                CancellationToken cancellationToken = default)
        {
            if (parameterNames.Count == 0)
            {
                throw new ArgumentException(
                    "En az bir parameter belirtilmelidir.",
                    nameof(parameterNames));
            }

            foreach (string parameterName in parameterNames)
            {
                ParameterDefinition definition =
                    _catalog.GetRequired(parameterName);

                if (!definition.CanRead)
                {
                    throw new ParameterValidationException(
                        parameterName,
                        $"'{parameterName}' parameterı okunamaz.");
                }
            }

            CommandResponse response =
                await _commandService.GetAsync(
                    parameterNames,
                    cancellationToken);

            return ConvertResponse(response);
        }

        public async Task<ParameterOperationResult> SetAsync(
            string parameterName,
            object value,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<string, ParameterOperationResult> results =
                await SetAsync(
                    new Dictionary<string, object>
                    {
                        [parameterName] = value
                    },
                    cancellationToken);

            return results[parameterName];
        }

        public async Task<
            IReadOnlyDictionary<string, ParameterOperationResult>>
            SetAsync(
                IReadOnlyDictionary<string, object> parameters,
                CancellationToken cancellationToken = default)
        {
            if (parameters.Count == 0)
            {
                throw new ArgumentException(
                    "En az bir parameter belirtilmelidir.",
                    nameof(parameters));
            }

            Dictionary<string, object?> normalizedParameters =
                new(StringComparer.Ordinal);

            foreach (
                KeyValuePair<string, object> parameter
                in parameters)
            {
                ParameterDefinition definition =
                    _catalog.GetRequired(parameter.Key);

                if (!definition.CanWrite)
                {
                    throw new ParameterValidationException(
                        parameter.Key,
                        $"'{parameter.Key}' parameterı yazılamaz.");
                }

                object normalizedValue =
                    NormalizeAndValidateValue(
                        definition,
                        parameter.Value);

                normalizedParameters.Add(
                    parameter.Key,
                    normalizedValue);
            }

            CommandResponse response =
                await _commandService.SetAsync(
                    normalizedParameters,
                    cancellationToken);

            return ConvertResponse(response);
        }

        private static IReadOnlyDictionary<
            string,
            ParameterOperationResult> ConvertResponse(
                CommandResponse response)
        {
            Dictionary<string, ParameterOperationResult> result =
                new(StringComparer.Ordinal);

            foreach (
                KeyValuePair<string, CommandParameterResult> parameter
                in response.Parameters)
            {
                result.Add(
                    parameter.Key,
                    new ParameterOperationResult(
                        parameter.Key,
                        parameter.Value.Status,
                        parameter.Value.Value));
            }

            return result;
        }

        private static object NormalizeAndValidateValue(
            ParameterDefinition definition,
            object value)
        {
            object normalized =
                definition.ValueType switch
                {
                    ParameterValueType.Integer =>
                        NormalizeInteger(
                            definition.Name,
                            value),

                    ParameterValueType.FloatingPoint =>
                        NormalizeFloatingPoint(
                            definition.Name,
                            value),

                    ParameterValueType.String =>
                        NormalizeString(
                            definition.Name,
                            value),

                    ParameterValueType.Boolean =>
                        NormalizeBoolean(
                            definition.Name,
                            value),

                    _ =>
                        throw new ParameterValidationException(
                            definition.Name,
                            "Desteklenmeyen parameter veri tipi.")
                };

            ValidateRange(
                definition,
                normalized);

            return normalized;
        }

        private static int NormalizeInteger(
            string parameterName,
            object value)
        {
            try
            {
                double numericValue =
                    Convert.ToDouble(
                        value,
                        CultureInfo.InvariantCulture);

                if (double.IsNaN(numericValue) ||
                    double.IsInfinity(numericValue) ||
                    numericValue != Math.Truncate(numericValue))
                {
                    throw new ParameterValidationException(
                        parameterName,
                        $"'{parameterName}' tam sayı olmalıdır.");
                }

                if (numericValue < int.MinValue ||
                    numericValue > int.MaxValue)
                {
                    throw new ParameterValidationException(
                        parameterName,
                        $"'{parameterName}' Int32 aralığı dışındadır.");
                }

                return checked((int)numericValue);
            }
            catch (ParameterValidationException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                throw new ParameterValidationException(
                    parameterName,
                    $"'{parameterName}' için geçerli bir tam sayı gereklidir.");
            }
        }

        private static double NormalizeFloatingPoint(
            string parameterName,
            object value)
        {
            try
            {
                double numericValue =
                    Convert.ToDouble(
                        value,
                        CultureInfo.InvariantCulture);

                if (double.IsNaN(numericValue) ||
                    double.IsInfinity(numericValue))
                {
                    throw new ParameterValidationException(
                        parameterName,
                        $"'{parameterName}' sonlu bir sayı olmalıdır.");
                }

                return numericValue;
            }
            catch (ParameterValidationException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is
                    FormatException or
                    InvalidCastException or
                    OverflowException)
            {
                throw new ParameterValidationException(
                    parameterName,
                    $"'{parameterName}' için geçerli bir sayı gereklidir.");
            }
        }

        private static string NormalizeString(
            string parameterName,
            object value)
        {
            if (value is not string stringValue)
            {
                throw new ParameterValidationException(
                    parameterName,
                    $"'{parameterName}' string olmalıdır.");
            }

            return stringValue;
        }

        private static bool NormalizeBoolean(
            string parameterName,
            object value)
        {
            if (value is not bool boolValue)
            {
                throw new ParameterValidationException(
                    parameterName,
                    $"'{parameterName}' boolean olmalıdır.");
            }

            return boolValue;
        }

        private static void ValidateRange(
            ParameterDefinition definition,
            object normalizedValue)
        {
            if (definition.Minimum is null &&
                definition.Maximum is null)
            {
                return;
            }

            if (definition.ValueType is not
                (ParameterValueType.Integer or
                 ParameterValueType.FloatingPoint))
            {
                return;
            }

            double numericValue =
                Convert.ToDouble(
                    normalizedValue,
                    CultureInfo.InvariantCulture);

            if (definition.Minimum is not null &&
                numericValue < definition.Minimum.Value)
            {
                throw new ParameterValidationException(
                    definition.Name,
                    $"'{definition.Name}' minimum değeri " +
                    $"{definition.Minimum.Value} olmalıdır.");
            }

            if (definition.Maximum is not null &&
                numericValue > definition.Maximum.Value)
            {
                throw new ParameterValidationException(
                    definition.Name,
                    $"'{definition.Name}' maksimum değeri " +
                    $"{definition.Maximum.Value} olmalıdır.");
            }
        }
    }
}