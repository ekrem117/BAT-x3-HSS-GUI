using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.Polling;
using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Tests.Parameters
{
    public sealed class ParameterPollingProfileTests
    {
        [Fact]
        public void StatusParameters_ShouldContainExpectedParameters()
        {
            Assert.Contains(ParameterNames.System.Mode, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.System.Link, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.System.Fps, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.Vision.DetectionCount, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.Vision.InferenceMilliseconds, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.Weapon.Ammo, ParameterPollingProfile.StatusParameters);

            Assert.Contains(ParameterNames.Weapon.ShotsFired, ParameterPollingProfile.StatusParameters);
        }

        [Fact]
        public void StatusParameters_ShouldContainOnlyReadableParameters()
        {
            StaticParameterCatalog catalog = new();

            foreach (
                string parameterName
                in ParameterPollingProfile.StatusParameters)
            {
                ParameterDefinition definition = catalog.GetRequired(parameterName);

                Assert.NotEqual(ParameterAccessType.WriteOnly, definition.AccessType);
            }
        }

        [Theory]
        [InlineData(ParameterNames.Motion.ManualUp)]
        [InlineData(ParameterNames.Motion.ManualDown)]
        [InlineData(ParameterNames.Motion.ManualLeft)]
        [InlineData(ParameterNames.Motion.ManualRight)]
        public void StatusParameters_ShouldNotContainManualMotionParameters(string parameterName)
        {
            Assert.DoesNotContain(parameterName, ParameterPollingProfile.StatusParameters);
        }

        [Fact]
        public void StatusParameters_ShouldNotContainWeaponFire()
        {
            Assert.DoesNotContain(ParameterNames.Weapon.Fire, ParameterPollingProfile.StatusParameters);
        }

        [Fact]
        public void StatusParameters_ShouldContainUniqueParameters()
        {
            string[] duplicates =
                ParameterPollingProfile.StatusParameters
                    .GroupBy(parameterName => parameterName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToArray();

            Assert.Empty(duplicates);
        }

        [Theory]
        [InlineData(ParameterNames.Weapon.Armed)]
        [InlineData(ParameterNames.Weapon.FireMode)]
        [InlineData(ParameterNames.Weapon.BurstCount)]
        [InlineData(ParameterNames.Weapon.Selected)]
        [InlineData(ParameterNames.Weapon.Ammo)]
        [InlineData(ParameterNames.Weapon.ShotsFired)]
        public void StatusParameters_ShouldContainWeaponState(string parameterName)
        {
            Assert.Contains(parameterName,
                ParameterPollingProfile.StatusParameters);
        }

        [Fact]
        public void StatusParameters_ShouldNotContainWeaponFireTrigger()
        {
            Assert.DoesNotContain(ParameterNames.Weapon.Fire, ParameterPollingProfile.StatusParameters);
        }

        [Fact]
        public void StatusParameters_ShouldNotContainDuplicates()
        {
            int uniqueCount = ParameterPollingProfile.StatusParameters
                    .Distinct(StringComparer.Ordinal)
                    .Count();

            Assert.Equal(ParameterPollingProfile.StatusParameters.Count, uniqueCount);
        }
    }
}