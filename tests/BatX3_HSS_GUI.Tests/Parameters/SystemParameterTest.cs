using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Tests.Parameters
{
    public sealed class SystemParameterTest
    {
        [Fact]
        public void SystemParameters_ShouldExist()
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition mode =
                catalog.GetRequired(
                    ParameterNames.System.Mode);

            ParameterDefinition link =
                catalog.GetRequired(
                    ParameterNames.System.Link);

            Assert.Equal(
                ParameterAccessType.ReadWrite,
                mode.AccessType);

            Assert.Equal(
                ParameterValueType.Integer,
                mode.ValueType);

            Assert.Equal(
                0,
                mode.Minimum);

            Assert.Equal(
                4,
                mode.Maximum);

            Assert.Equal(
                ParameterAccessType.ReadOnly,
                link.AccessType);

            Assert.Equal(
                0,
                link.Minimum);

            Assert.Equal(
                2,
                link.Maximum);
        }

        [Theory]
        [InlineData(ParameterNames.Motion.ManualUp)]
        [InlineData(ParameterNames.Motion.ManualDown)]
        [InlineData(ParameterNames.Motion.ManualLeft)]
        [InlineData(ParameterNames.Motion.ManualRight)]
        public void ManualMotionParameters_ShouldBeReadWriteStateParameters(
            string parameterName)
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    parameterName);

            Assert.Equal(
                ParameterAccessType.ReadWrite,
                parameter.AccessType);

            Assert.Equal(
                ParameterValueType.Integer,
                parameter.ValueType);

            Assert.Equal(
                0,
                parameter.Minimum);

            Assert.Equal(
                1,
                parameter.Maximum);

            Assert.False(
                parameter.IsTrigger);
        }

        [Theory]
        [InlineData(ParameterNames.Motion.AnalogPan)]
        [InlineData(ParameterNames.Motion.AnalogTilt)]
        public void AnalogMotionParameters_ShouldBeReadWriteFloats(
            string parameterName)
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    parameterName);

            Assert.Equal(
                ParameterAccessType.ReadWrite,
                parameter.AccessType);

            Assert.Equal(
                ParameterValueType.FloatingPoint,
                parameter.ValueType);

            Assert.Equal(
                -1.0,
                parameter.Minimum);

            Assert.Equal(
                1.0,
                parameter.Maximum);

            Assert.False(
                parameter.IsTrigger);
        }

        [Fact]
        public void AnalogPrecision_ShouldBeReadWriteBinaryState()
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    ParameterNames.Motion.AnalogPrecision);

            Assert.Equal(
                ParameterAccessType.ReadWrite,
                parameter.AccessType);

            Assert.Equal(
                ParameterValueType.Integer,
                parameter.ValueType);

            Assert.Equal(
                0,
                parameter.Minimum);

            Assert.Equal(
                1,
                parameter.Maximum);

            Assert.False(
                parameter.IsTrigger);
        }

        [Theory]
        [InlineData(ParameterNames.Motion.AutoPan)]
        [InlineData(ParameterNames.Motion.AutoTilt)]
        public void AutoMotionParameters_ShouldBeReadOnlyFloats(
            string parameterName)
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    parameterName);

            Assert.Equal(
                ParameterAccessType.ReadOnly,
                parameter.AccessType);

            Assert.Equal(
                ParameterValueType.FloatingPoint,
                parameter.ValueType);

            Assert.Equal(
                -1.0,
                parameter.Minimum);

            Assert.Equal(
                1.0,
                parameter.Maximum);
        }

        [Fact]
        public void WeaponArmed_ShouldBeReadWriteBinaryState()
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    ParameterNames.Weapon.Armed);

            Assert.Equal(
                ParameterAccessType.ReadWrite,
                parameter.AccessType);

            Assert.Equal(
                ParameterValueType.Integer,
                parameter.ValueType);

            Assert.Equal(
                0,
                parameter.Minimum);

            Assert.Equal(
                1,
                parameter.Maximum);
        }

        [Fact]
        public void WeaponFire_ShouldRemainWriteOnlyTrigger()
        {
            StaticParameterCatalog catalog =
                new();

            ParameterDefinition parameter =
                catalog.GetRequired(
                    ParameterNames.Weapon.Fire);

            Assert.Equal(
                ParameterAccessType.WriteOnly,
                parameter.AccessType);

            Assert.True(
                parameter.IsTrigger);
        }

        [Theory]
        [InlineData("turret.pan")]
        [InlineData("turret.tilt")]
        [InlineData("turret.home")]
        [InlineData("system.estop")]
        public void PlannedParameters_ShouldNotBeActive(
            string parameterName)
        {
            StaticParameterCatalog catalog =
                new();

            Assert.False(
                catalog.TryGet(
                    parameterName,
                    out _));
        }
    }
}
