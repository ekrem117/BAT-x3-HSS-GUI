using BatX3_HSS_GUI.Domain.Communication.Command;
using System.Text.Json;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Command
{
    internal static class CommandMessageParser
    {
        public static bool TryParse(
            ReadOnlyMemory<byte> data,
            out CommandResponse? response,
            out string? error)
        {
            response = null;
            error = null;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(data);

                JsonElement root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    error = "Command response JSON object değildir.";
                    return false;
                }

                if (!root.TryGetProperty(
                        "Method",
                        out JsonElement methodElement) ||
                    methodElement.ValueKind != JsonValueKind.String)
                {
                    error = "Command response Method alanı geçersiz.";
                    return false;
                }

                string? methodText =
                    methodElement.GetString();

                if (!TryParseMethod(
                        methodText,
                        out CommandResponseMethod method))
                {
                    error =
                        $"Bilinmeyen response Method: {methodText}";
                    return false;
                }

                int? messageId =
                    ParseMessageId(root);

                if (method != CommandResponseMethod.ErrorResponse &&
                    messageId is null)
                {
                    error =
                        "GETRSP/SETRSP messageID içermelidir.";
                    return false;
                }

                if (method == CommandResponseMethod.ErrorResponse)
                {
                    return TryParseErrorResponse(
                        root,
                        messageId,
                        out response,
                        out error);
                }

                return TryParseParameterResponse(
                    root,
                    method,
                    messageId!.Value,
                    out response,
                    out error);
            }
            catch (JsonException exception)
            {
                error =
                    $"Command response JSON parse hatası: {exception.Message}";

                return false;
            }
        }

        private static bool TryParseMethod(
            string? value,
            out CommandResponseMethod method)
        {
            if (string.Equals(
                    value,
                    "GETRSP",
                    StringComparison.OrdinalIgnoreCase))
            {
                method = CommandResponseMethod.GetResponse;
                return true;
            }

            if (string.Equals(
                    value,
                    "SETRSP",
                    StringComparison.OrdinalIgnoreCase))
            {
                method = CommandResponseMethod.SetResponse;
                return true;
            }

            if (string.Equals(
                    value,
                    "ERRRSP",
                    StringComparison.OrdinalIgnoreCase))
            {
                method = CommandResponseMethod.ErrorResponse;
                return true;
            }

            method = default;
            return false;
        }

        private static int? ParseMessageId(JsonElement root)
        {
            if (!root.TryGetProperty(
                    "messageID",
                    out JsonElement messageIdElement))
            {
                return null;
            }

            if (messageIdElement.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (messageIdElement.ValueKind != JsonValueKind.Number ||
                !messageIdElement.TryGetInt32(out int messageId))
            {
                return null;
            }

            return messageId;
        }

        private static bool TryParseErrorResponse(
            JsonElement root,
            int? messageId,
            out CommandResponse? response,
            out string? error)
        {
            response = null;
            error = null;

            if (!root.TryGetProperty(
                    "status",
                    out JsonElement statusElement) ||
                !statusElement.TryGetInt32(out int status))
            {
                error = "ERRRSP status alanı geçersiz.";
                return false;
            }

            response = new CommandResponse
            {
                Method = CommandResponseMethod.ErrorResponse,
                MessageId = messageId,
                ErrorStatus = (CommandStatus)status
            };

            return true;
        }

        private static bool TryParseParameterResponse(
            JsonElement root,
            CommandResponseMethod method,
            int messageId,
            out CommandResponse? response,
            out string? error)
        {
            response = null;
            error = null;

            Dictionary<string, CommandParameterResult> parameters =
                new(StringComparer.Ordinal);

            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.NameEquals("Method") ||
                    property.NameEquals("messageID"))
                {
                    continue;
                }

                JsonElement parameterElement =
                    property.Value;

                if (parameterElement.ValueKind !=
                    JsonValueKind.Object)
                {
                    error =
                        $"'{property.Name}' sonucu JSON object değildir.";

                    return false;
                }

                if (!parameterElement.TryGetProperty(
                        "status",
                        out JsonElement statusElement) ||
                    !statusElement.TryGetInt32(out int statusCode))
                {
                    error =
                        $"'{property.Name}' status alanı geçersiz.";

                    return false;
                }

                if (!parameterElement.TryGetProperty(
                        "value",
                        out JsonElement valueElement))
                {
                    error =
                        $"'{property.Name}' value alanı eksik.";

                    return false;
                }

                CommandStatus status =
                    (CommandStatus)statusCode;

                object? value =
                    status == CommandStatus.Success
                        ? ConvertValue(valueElement)
                        : null;

                parameters.Add(
                    property.Name,
                    new CommandParameterResult(
                        status,
                        value));
            }

            response = new CommandResponse
            {
                Method = method,
                MessageId = messageId,
                Parameters = parameters
            };

            return true;
        }

        private static object? ConvertValue(
            JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.Null:
                    return null;

                case JsonValueKind.String:
                    return value.GetString();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Number:
                    {
                        if (value.TryGetInt32(out int intValue))
                        {
                            return intValue;
                        }

                        if (value.TryGetInt64(out long longValue))
                        {
                            return longValue;
                        }

                        return value.GetDouble();
                    }

                default:
                    return value.GetRawText();
            }
        }
    }
}