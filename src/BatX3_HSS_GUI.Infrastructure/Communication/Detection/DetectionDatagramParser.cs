using BatX3_HSS_GUI.Domain.Detection;
using System.Text.Json;

namespace BatX3_HSS_GUI.Infrastructure.Communication.Detection
{
    internal static class DetectionDatagramParser
    {
        public static bool TryParse(
            ReadOnlyMemory<byte> datagram,
            out DetectionFrame? frame,
            out string? error)
        {
            frame = null;
            error = null;

            if (datagram.IsEmpty)
            {
                error =
                    "Detection datagram boş.";

                return false;
            }

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(datagram);

                JsonElement root =
                    document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    error =
                        "Detection mesaj root değeri object olmalıdır.";

                    return false;
                }

                if (!TryGetUInt32(
                        root,
                        "frame_id",
                        out uint frameId))
                {
                    error =
                        "Detection frame_id eksik veya geçersiz.";

                    return false;
                }

                if (!TryGetDouble(
                        root,
                        "ts",
                        out double timestamp))
                {
                    error =
                        "Detection ts eksik veya geçersiz.";

                    return false;
                }

                if (double.IsNaN(timestamp) ||
                    double.IsInfinity(timestamp))
                {
                    error =
                        "Detection timestamp geçersiz.";

                    return false;
                }

                if (!root.TryGetProperty(
                        "detections",
                        out JsonElement detectionsElement) ||
                    detectionsElement.ValueKind != JsonValueKind.Array)
                {
                    error =
                        "Detection mesajında detections array bulunamadı.";

                    return false;
                }

                List<DetectionTarget> detections =
                    new();

                foreach (
                    JsonElement detectionElement
                    in detectionsElement.EnumerateArray())
                {
                    if (!TryParseDetection(
                            detectionElement,
                            out DetectionTarget? target,
                            out error))
                    {
                        return false;
                    }

                    if (target is not null)
                    {
                        detections.Add(target);
                    }
                }

                DetectionLockInfo? lockInfo =
                    null;

                if (root.TryGetProperty(
                        "lock",
                        out JsonElement lockElement) &&
                    lockElement.ValueKind != JsonValueKind.Null)
                {
                    if (lockElement.ValueKind != JsonValueKind.Object)
                    {
                        error =
                            "Detection lock alanı object veya null olmalıdır.";

                        return false;
                    }

                    lockInfo =
                        ParseLockInfo(
                            lockElement);
                }

                frame =
                    new DetectionFrame
                    {
                        FrameId =
                            frameId,

                        Timestamp =
                            timestamp,

                        Detections =
                            detections,

                        Lock =
                            lockInfo
                    };

                return true;
            }
            catch (JsonException exception)
            {
                error =
                    $"Detection JSON parse hatası: {exception.Message}";

                return false;
            }
            catch (Exception exception)
            {
                error =
                    $"Detection datagram parse hatası: {exception.Message}";

                return false;
            }
        }

        private static bool TryParseDetection(
            JsonElement element,
            out DetectionTarget? target,
            out string? error)
        {
            target = null;
            error = null;

            if (element.ValueKind != JsonValueKind.Object)
            {
                error =
                    "Detection array elemanı object olmalıdır.";

                return false;
            }

            if (!TryGetInt32(
                    element,
                    "id",
                    out int id))
            {
                error =
                    "Detection id eksik veya geçersiz.";

                return false;
            }

            if (!TryGetDouble(
                    element,
                    "x",
                    out double x) ||
                !TryGetDouble(
                    element,
                    "y",
                    out double y) ||
                !TryGetDouble(
                    element,
                    "w",
                    out double width) ||
                !TryGetDouble(
                    element,
                    "h",
                    out double height))
            {
                error =
                    $"Detection bbox geçersiz. Id={id}";

                return false;
            }

            if (width < 0 ||
                height < 0)
            {
                error =
                    $"Detection bbox boyutu negatif olamaz. Id={id}";

                return false;
            }

            if (!TryGetInt32(
                    element,
                    "cls",
                    out int classId))
            {
                error =
                    $"Detection cls geçersiz. Id={id}";

                return false;
            }

            if (!TryGetInt32(
                    element,
                    "team",
                    out int teamId))
            {
                error =
                    $"Detection team geçersiz. Id={id}";

                return false;
            }

            if (!TryGetDouble(
                    element,
                    "conf",
                    out double confidence))
            {
                error =
                    $"Detection conf geçersiz. Id={id}";

                return false;
            }

            if (!TryGetDouble(
                    element,
                    "vx",
                    out double velocityX) ||
                !TryGetDouble(
                    element,
                    "vy",
                    out double velocityY))
            {
                error =
                    $"Detection velocity geçersiz. Id={id}";

                return false;
            }

            if (!TryGetDouble(
                    element,
                    "age",
                    out double age))
            {
                error =
                    $"Detection age geçersiz. Id={id}";

                return false;
            }

            if (!TryGetBoolean(
                    element,
                    "predicted",
                    out bool predicted))
            {
                error =
                    $"Detection predicted geçersiz. Id={id}";

                return false;
            }

            int? parentId =
                GetNullableInt32(
                    element,
                    "parent_id");

            target =
                new DetectionTarget
                {
                    Id =
                        id,

                    X =
                        x,

                    Y =
                        y,

                    Width =
                        width,

                    Height =
                        height,

                    ClassId =
                        classId,

                    TeamId =
                        teamId,

                    Confidence =
                        confidence,

                    ParentId =
                        parentId,

                    VelocityX =
                        velocityX,

                    VelocityY =
                        velocityY,

                    Age =
                        age,

                    Predicted =
                        predicted
                };

            return true;
        }

        private static DetectionLockInfo ParseLockInfo(
            JsonElement element)
        {
            return new DetectionLockInfo
            {
                TrackId =
                    GetNullableInt32(
                        element,
                        "track_id"),

                State =
                    GetNullableString(
                        element,
                        "state"),

                StateName =
                    GetNullableString(
                        element,
                        "state_name"),

                ClassId =
                    GetNullableInt32(
                        element,
                        "cls"),

                TeamId =
                    GetNullableInt32(
                        element,
                        "team"),

                X =
                    GetNullableDouble(
                        element,
                        "x"),

                Y =
                    GetNullableDouble(
                        element,
                        "y"),

                VelocityX =
                    GetNullableDouble(
                        element,
                        "vx"),

                VelocityY =
                    GetNullableDouble(
                        element,
                        "vy"),

                LostFor =
                    GetNullableDouble(
                        element,
                        "lost_for"),

                ShotsFired =
                    GetNullableInt32(
                        element,
                        "shots_fired"),

                Reacquires =
                    GetNullableInt32(
                        element,
                        "reacquires"),

                Duration =
                    GetNullableDouble(
                        element,
                        "duration")
            };
        }

        private static bool TryGetUInt32(
            JsonElement element,
            string propertyName,
            out uint value)
        {
            value = default;

            return
                element.TryGetProperty(
                    propertyName,
                    out JsonElement property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetUInt32(
                    out value);
        }

        private static bool TryGetInt32(
            JsonElement element,
            string propertyName,
            out int value)
        {
            value = default;

            return
                element.TryGetProperty(
                    propertyName,
                    out JsonElement property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt32(
                    out value);
        }

        private static bool TryGetDouble(
            JsonElement element,
            string propertyName,
            out double value)
        {
            value = default;

            return
                element.TryGetProperty(
                    propertyName,
                    out JsonElement property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetDouble(
                    out value);
        }

        private static bool TryGetBoolean(
            JsonElement element,
            string propertyName,
            out bool value)
        {
            value = default;

            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }

            return false;
        }

        private static int? GetNullableInt32(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt32(
                    out int value))
            {
                return value;
            }

            return null;
        }

        private static double? GetNullableDouble(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number &&
                property.TryGetDouble(
                    out double value))
            {
                return value;
            }

            return null;
        }

        private static string? GetNullableString(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement property) ||
                property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }
    }
}