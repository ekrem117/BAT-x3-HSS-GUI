using System.Globalization;
using System.Text.Json;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Command
{
    internal static class CommandMessageSerializer
    {
        private const string MethodPropertyName =
            "Method";

        private const string MessageIdPropertyName =
            "messageID";

        public static byte[] SerializeGet(
            int messageId,
            IReadOnlyCollection<string> parameterNames)
        {
            if (parameterNames.Count == 0)
            {
                throw new ArgumentException(
                    "GET request en az bir parametre içermelidir.",
                    nameof(parameterNames));
            }

            using MemoryStream stream =
                new();

            using Utf8JsonWriter writer =
                new(stream);

            writer.WriteStartObject();

            writer.WriteString(
                MethodPropertyName,
                "GET");

            writer.WriteNumber(
                MessageIdPropertyName,
                messageId);

            HashSet<string> uniqueNames =
                new(
                    StringComparer.Ordinal);

            foreach (
                string parameterName
                in parameterNames)
            {
                ValidateParameterName(
                    parameterName);

                if (!uniqueNames.Add(
                        parameterName))
                {
                    throw new ArgumentException(
                        $"GET request duplicate parameter içeriyor: " +
                        $"{parameterName}",
                        nameof(parameterNames));
                }

                writer.WriteString(
                    parameterName,
                    "?");
            }

            writer.WriteEndObject();
            writer.Flush();

            return stream.ToArray();
        }

        public static byte[] SerializeSet(
            int messageId,
            IReadOnlyDictionary<
                string,
                object?> parameters)
        {
            if (parameters.Count == 0)
            {
                throw new ArgumentException(
                    "SET request en az bir parametre içermelidir.",
                    nameof(parameters));
            }

            using MemoryStream stream =
                new();

            using Utf8JsonWriter writer =
                new(stream);

            writer.WriteStartObject();

            writer.WriteString(
                MethodPropertyName,
                "SET");

            writer.WriteNumber(
                MessageIdPropertyName,
                messageId);

            foreach (
                KeyValuePair<string, object?> parameter
                in parameters)
            {
                ValidateParameterName(
                    parameter.Key);

                writer.WritePropertyName(
                    parameter.Key);

                WriteParameterValue(
                    writer,
                    parameter.Value);
            }

            writer.WriteEndObject();
            writer.Flush();

            return stream.ToArray();
        }

        private static void WriteParameterValue(
            Utf8JsonWriter writer,
            object? value)
        {
            if (value is null)
            {
                writer.WriteNullValue();

                return;
            }

            switch (value)
            {
                /*
                 * Python command server parameter tiplerini
                 * isinstance(...) ile katı biçimde kontrol ediyor.
                 *
                 * System.Text.Json bazı tam değerli floating-point
                 * sayılarını:
                 *
                 *     0.0 -> 0
                 *     1.0 -> 1
                 *
                 * şeklinde serialize edebilir.
                 *
                 * Python JSON parser bu değerleri int olarak oluşturduğu
                 * için float bekleyen parametreler reddedilir.
                 *
                 * Bu nedenle CLR tarafındaki double/float değerlerin
                 * JSON wire representation'ında da floating-point
                 * olarak kalmasını garanti ediyoruz.
                 */
                case double doubleValue:
                    {
                        WriteFloatingPointValue(
                            writer,
                            doubleValue);

                        return;
                    }

                case float floatValue:
                    {
                        WriteFloatingPointValue(
                            writer,
                            floatValue);

                        return;
                    }

                default:
                    {
                        JsonSerializer.Serialize(
                            writer,
                            value,
                            value.GetType());

                        return;
                    }
            }
        }

        private static void WriteFloatingPointValue(
            Utf8JsonWriter writer,
            double value)
        {
            if (!double.IsFinite(
                    value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "JSON protokolünde NaN veya Infinity " +
                    "floating-point değer gönderilemez.");
            }

            string text =
                value.ToString(
                    "R",
                    CultureInfo.InvariantCulture);

            WriteFloatingPointLiteral(
                writer,
                text);
        }

        private static void WriteFloatingPointValue(
            Utf8JsonWriter writer,
            float value)
        {
            if (!float.IsFinite(
                    value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "JSON protokolünde NaN veya Infinity " +
                    "floating-point değer gönderilemez.");
            }

            string text =
                value.ToString(
                    "R",
                    CultureInfo.InvariantCulture);

            WriteFloatingPointLiteral(
                writer,
                text);
        }

        private static void WriteFloatingPointLiteral(
            Utf8JsonWriter writer,
            string text)
        {
            /*
             * Exponent içeren değer zaten JSON parser tarafından
             * floating-point olarak yorumlanır:
             *
             *     1E-05
             *
             * Normal tam sayı görünümündeki floating-point değerlerde
             * ise açıkça decimal point ekliyoruz:
             *
             *     0   -> 0.0
             *     1   -> 1.0
             *    -1   -> -1.0
             */
            bool hasDecimalPoint =
                text.IndexOf(
                    '.') >= 0;

            bool hasExponent =
                text.IndexOf(
                    'E') >= 0 ||
                text.IndexOf(
                    'e') >= 0;

            if (!hasDecimalPoint &&
                !hasExponent)
            {
                text +=
                    ".0";
            }

            writer.WriteRawValue(
                text,
                skipInputValidation: false);
        }

        private static void ValidateParameterName(
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    parameterName))
            {
                throw new ArgumentException(
                    "Parameter name boş olamaz.");
            }

            if (string.Equals(
                    parameterName,
                    MethodPropertyName,
                    StringComparison.Ordinal) ||
                string.Equals(
                    parameterName,
                    MessageIdPropertyName,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"'{parameterName}' rezerve protokol alanıdır.");
            }
        }
    }
}