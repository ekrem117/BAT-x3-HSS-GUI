using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Tests.Parameters
{
    public sealed class StaticParameterCatalogTests
    {
        private readonly StaticParameterCatalog _catalog = new();

        [Fact]
        public void ControlPidKp_ShouldBeReadWriteFloat()
        {
            ParameterDefinition definition = _catalog.GetRequired(ParameterNames.Control.PidKp);

            Assert.Equal(ParameterValueType.FloatingPoint, definition.ValueType);

            Assert.Equal(ParameterAccessType.ReadWrite, definition.AccessType);

            Assert.Equal(0.0, definition.Minimum);

            Assert.Equal(10.0, definition.Maximum);

            Assert.True(definition.CanRead);
            Assert.True(definition.CanWrite);
            Assert.False(definition.IsTrigger);
        }

        [Fact]
        public void WeaponFire_ShouldBeWriteOnlyTrigger()
        {
            ParameterDefinition definition = _catalog.GetRequired(ParameterNames.Weapon.Fire);

            Assert.Equal(ParameterAccessType.WriteOnly, definition.AccessType);

            Assert.False(definition.CanRead);
            Assert.True(definition.CanWrite);
            Assert.True(definition.IsTrigger);
        }
    }
}