using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Domain.System;

namespace BatX3_HSS_GUI.Tests.Actions
{
    public sealed class ApplicationActionModeMetadataTests
    {
        [Theory]
        [InlineData(ApplicationActionIds.MotionUp)]
        [InlineData(ApplicationActionIds.MotionDown)]
        [InlineData(ApplicationActionIds.MotionLeft)]
        [InlineData(ApplicationActionIds.MotionRight)]
        public void MotionActions_ShouldAllowOnlyManualMode(string actionId)
        {
            StaticApplicationActionCatalog catalog = new();

            ApplicationActionDefinition action = catalog.GetRequired(actionId);

            SystemOperatingMode allowedMode = Assert.Single(action.AllowedOperatingModes);

            Assert.Equal(SystemOperatingMode.Manual, allowedMode);
        }

        [Fact]
        public void WeaponFire_ShouldNotReceiveManualOnlyRestriction()
        {
            StaticApplicationActionCatalog catalog = new();

            ApplicationActionDefinition action = catalog.GetRequired(ApplicationActionIds.WeaponFire);

            Assert.Empty(action.AllowedOperatingModes);
        }
    }
}