using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Application.Parameters
{
    public sealed class StaticParameterCatalog :
            IParameterCatalog
    {
        private readonly IReadOnlyDictionary<
            string,
            ParameterDefinition> _parameters;

        public StaticParameterCatalog()
        {
            ParameterDefinition[] definitions =
            [
                // =========================================================
                // VISION - ENEMY
                // =========================================================

                CreateInteger(
                    ParameterNames.Vision.EnemyHMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.EnemyHMax,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.EnemyHMin2,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.EnemyHMax2,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.EnemySMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    255),

                CreateInteger(
                    ParameterNames.Vision.EnemyVMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    255),

                // =========================================================
                // VISION - FRIEND
                // =========================================================

                CreateInteger(
                    ParameterNames.Vision.FriendHMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.FriendHMax,
                    ParameterAccessType.ReadWrite,
                    0,
                    179),

                CreateInteger(
                    ParameterNames.Vision.FriendSMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    255),

                CreateInteger(
                    ParameterNames.Vision.FriendVMin,
                    ParameterAccessType.ReadWrite,
                    0,
                    255),

                // =========================================================
                // VISION - DETECTION
                // =========================================================

                CreateFloat(
                    ParameterNames.Vision.ConfidenceThreshold,
                    ParameterAccessType.ReadWrite,
                    0.0,
                    1.0),

                CreateInteger(
                    ParameterNames.Vision.DetectionCount,
                    ParameterAccessType.ReadOnly,
                    0,
                    100,
                    "adet"),

                CreateFloat(
                    ParameterNames.Vision.InferenceMilliseconds,
                    ParameterAccessType.ReadOnly,
                    0.0,
                    10000.0,
                    "ms"),

                // =========================================================
                // CONTROL
                // =========================================================

                CreateFloat(
                    ParameterNames.Control.PidKp,
                    ParameterAccessType.ReadWrite,
                    0.0,
                    10.0),

                // =========================================================
                // SYSTEM
                // =========================================================

                CreateFloat(
                    ParameterNames.System.Fps,
                    ParameterAccessType.ReadOnly,
                    0.0,
                    200.0,
                    "fps"),

                CreateInteger(
                    ParameterNames.System.Mode,
                    ParameterAccessType.ReadWrite,
                    0,
                    4),

                CreateInteger(
                    ParameterNames.System.Link,
                    ParameterAccessType.ReadOnly,
                    0,
                    2),

                // =========================================================
                // MOTION - MANUAL DIGITAL
                // =========================================================

                CreateInteger(
                    ParameterNames.Motion.ManualUp,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                CreateInteger(
                    ParameterNames.Motion.ManualDown,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                CreateInteger(
                    ParameterNames.Motion.ManualRight,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                CreateInteger(
                    ParameterNames.Motion.ManualLeft,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                // =========================================================
                // MOTION - ANALOG GAMEPAD
                // =========================================================

                CreateFloat(
                    ParameterNames.Motion.AnalogPan,
                    ParameterAccessType.ReadWrite,
                    -1.0,
                    1.0),

                CreateFloat(
                    ParameterNames.Motion.AnalogTilt,
                    ParameterAccessType.ReadWrite,
                    -1.0,
                    1.0),

                CreateInteger(
                    ParameterNames.Motion.AnalogPrecision,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                // =========================================================
                // MOTION - AUTO
                // =========================================================

                CreateFloat(
                    ParameterNames.Motion.AutoPan,
                    ParameterAccessType.ReadOnly,
                    -1.0,
                    1.0),

                CreateFloat(
                    ParameterNames.Motion.AutoTilt,
                    ParameterAccessType.ReadOnly,
                    -1.0,
                    1.0),

                // =========================================================
                // WEAPON
                // =========================================================

                CreateInteger(
                    ParameterNames.Weapon.Armed,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                CreateInteger(
                    ParameterNames.Weapon.FireMode,
                    ParameterAccessType.ReadWrite,
                    0,
                    1),

                CreateInteger(
                    ParameterNames.Weapon.BurstCount,
                    ParameterAccessType.ReadWrite,
                    1,
                    10,
                    "adet"),

                CreateInteger(
                    ParameterNames.Weapon.Selected,
                    ParameterAccessType.ReadWrite,
                    0,
                    2),

                CreateInteger(
                    ParameterNames.Weapon.Ammo,
                    ParameterAccessType.ReadOnly,
                    0,
                    100,
                    "adet"),

                CreateInteger(
                    ParameterNames.Weapon.ShotsFired,
                    ParameterAccessType.ReadOnly,
                    0,
                    9999,
                    "adet"),

                CreateInteger(
                    ParameterNames.Weapon.Fire,
                    ParameterAccessType.WriteOnly,
                    0,
                    1,
                    isTrigger: true)
            ];

            _parameters =
                definitions.ToDictionary(
                    definition =>
                        definition.Name,
                    StringComparer.Ordinal);
        }

        public IReadOnlyCollection<ParameterDefinition>
            GetAll()
        {
            return _parameters
                .Values
                .ToArray();
        }

        public bool TryGet(
            string parameterName,
            out ParameterDefinition? definition)
        {
            return _parameters.TryGetValue(
                parameterName,
                out definition);
        }

        public ParameterDefinition GetRequired(
            string parameterName)
        {
            if (!_parameters.TryGetValue(
                    parameterName,
                    out ParameterDefinition? definition))
            {
                throw new KeyNotFoundException(
                    $"Parameter katalogda bulunamadı: {parameterName}");
            }

            return definition;
        }

        private static ParameterDefinition CreateInteger(
            string name,
            ParameterAccessType accessType,
            int minimum,
            int maximum,
            string? unit = null,
            bool isTrigger = false)
        {
            return new ParameterDefinition
            {
                Name =
                    name,

                ValueType =
                    ParameterValueType.Integer,

                AccessType =
                    accessType,

                Minimum =
                    minimum,

                Maximum =
                    maximum,

                Unit =
                    unit,

                IsTrigger =
                    isTrigger
            };
        }

        private static ParameterDefinition CreateFloat(
            string name,
            ParameterAccessType accessType,
            double minimum,
            double maximum,
            string? unit = null)
        {
            return new ParameterDefinition
            {
                Name =
                    name,

                ValueType =
                    ParameterValueType.FloatingPoint,

                AccessType =
                    accessType,

                Minimum =
                    minimum,

                Maximum =
                    maximum,

                Unit =
                    unit
            };
        }
    }
}