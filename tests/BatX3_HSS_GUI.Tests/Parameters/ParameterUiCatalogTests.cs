using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.UI;

namespace BatX3_HSS_GUI.Tests.Parameters
{
    public sealed class ParameterUiCatalogTests
    {
        [Fact]
        public void AllUiDefinitions_ShouldReferenceKnownParameters()
        {
            StaticParameterCatalog parameterCatalog = new();

            StaticParameterUiCatalog uiCatalog = new();

            foreach (ParameterUiDefinition uiDefinition in uiCatalog.GetAll())
            {
                bool exists = parameterCatalog.TryGet(uiDefinition.ParameterName, out _);

                Assert.True(exists, $"UI tanımı katalogda olmayan parametreyi kullanıyor: " + $"{uiDefinition.ParameterName}");
            }
        }

        [Fact]
        public void UiDefinitions_ShouldHaveUniqueParameterNames()
        {
            StaticParameterUiCatalog catalog = new();

            string[] duplicates =
                catalog
                    .GetAll()
                    .GroupBy(
                        definition =>
                            definition.ParameterName,
                        StringComparer.Ordinal)
                    .Where(
                        group =>
                            group.Count() > 1)
                    .Select(
                        group =>
                            group.Key)
                    .ToArray();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void OperationPage_ShouldContainExpectedParameters()
        {
            StaticParameterUiCatalog catalog = new();

            string[] names =
                catalog
                    .GetByPage(
                        ParameterUiPage.Operation)
                    .Select(
                        definition =>
                            definition.ParameterName)
                    .ToArray();

            Assert.Contains(ParameterNames.System.Link, names);

            Assert.Contains(ParameterNames.System.Fps, names);

            Assert.Contains(ParameterNames.Vision.DetectionCount, names);

            Assert.Contains(ParameterNames.Vision.InferenceMilliseconds, names);

            Assert.Contains(ParameterNames.Vision.ConfidenceThreshold, names);

            Assert.Contains(ParameterNames.Weapon.Armed, names);

            Assert.Contains(ParameterNames.Weapon.FireMode, names);

            Assert.Contains(ParameterNames.Weapon.BurstCount, names);

            Assert.Contains(ParameterNames.Weapon.Selected, names);

            Assert.Contains(ParameterNames.Weapon.Ammo, names);

            Assert.Contains(ParameterNames.Weapon.ShotsFired, names);
        }

        [Fact]
        public void ConfigurationPage_ShouldContainCalibrationAndControlParameters()
        {
            StaticParameterUiCatalog catalog = new();

            string[] names =
                catalog
                    .GetByPage(
                        ParameterUiPage.Configuration)
                    .Select(
                        definition =>
                            definition.ParameterName)
                    .ToArray();

            Assert.Contains(ParameterNames.Vision.EnemyHMin, names);

            Assert.Contains(ParameterNames.Vision.EnemyHMax, names);

            Assert.Contains(ParameterNames.Vision.EnemyHMin2, names);

            Assert.Contains(ParameterNames.Vision.EnemyHMax2, names);

            Assert.Contains(ParameterNames.Vision.EnemySMin, names);

            Assert.Contains(ParameterNames.Vision.EnemyVMin, names);

            Assert.Contains(ParameterNames.Vision.FriendHMin, names);

            Assert.Contains(ParameterNames.Vision.FriendHMax, names);

            Assert.Contains(ParameterNames.Vision.FriendSMin, names);

            Assert.Contains(ParameterNames.Vision.FriendVMin, names);

            Assert.Contains(ParameterNames.Control.PidKp, names);
        }

        [Theory]
        [InlineData(ParameterNames.System.Mode)]
        [InlineData(ParameterNames.Motion.ManualUp)]
        [InlineData(ParameterNames.Motion.ManualDown)]
        [InlineData(ParameterNames.Motion.ManualLeft)]
        [InlineData(ParameterNames.Motion.ManualRight)]
        [InlineData(ParameterNames.Weapon.Fire)]
        public void SpecialInteractionParameters_ShouldNotBeInGenericUiCatalog(
            string parameterName)
        {
            StaticParameterUiCatalog catalog = new();

            bool exists = catalog.TryGet(parameterName, out _);

            Assert.False(exists);
        }

        [Fact]
        public void WeaponArmed_ShouldUseTurkishBinaryOptions()
        {
            StaticParameterUiCatalog catalog = new();

            ParameterUiDefinition definition = catalog.GetRequired(ParameterNames.Weapon.Armed);

            Assert.Equal(ParameterUiPage.Operation, definition.Page);

            Assert.Equal(ParameterControlType.Select, definition.ControlType);

            Assert.Equal(2, definition.Options.Count);

            Assert.Contains(definition.Options, option => option.Value == 0 && option.DisplayName == "Kapalı");

            Assert.Contains(definition.Options, option => option.Value == 1 && option.DisplayName == "Açık");
        }

        [Fact]
        public void WeaponSelection_ShouldContainThreeTurkishOptions()
        {
            StaticParameterUiCatalog catalog = new();

            ParameterUiDefinition definition = catalog.GetRequired(ParameterNames.Weapon.Selected);

            Assert.Equal(3, definition.Options.Count);

            Assert.Contains(definition.Options, option => option.Value == 0 && option.DisplayName == "Sol");

            Assert.Contains(definition.Options, option => option.Value == 1 && option.DisplayName == "Sağ");

            Assert.Contains(definition.Options, option => option.Value == 2 && option.DisplayName == "Her İkisi");
        }

        [Fact]
        public void SystemLink_ShouldContainExpectedTurkishOptions()
        {
            StaticParameterUiCatalog catalog = new();

            ParameterUiDefinition definition = catalog.GetRequired(ParameterNames.System.Link);

            Assert.Equal(ParameterUiPage.Operation, definition.Page);

            Assert.Equal(ParameterControlType.ReadOnly, definition.ControlType);

            Assert.Equal(3, definition.Options.Count);

            Assert.Contains(definition.Options, option => option.Value == 0 && option.DisplayName == "Bağlantı Yok");

            Assert.Contains(definition.Options, option => option.Value == 1 && option.DisplayName == "Bağlı");

            Assert.Contains(definition.Options, option => option.Value == 2 && option.DisplayName == "Zaman Aşımı");
        }

        [Fact]
        public void AllUiDefinitions_ShouldHaveTurkishDisplayMetadata()
        {
            StaticParameterUiCatalog catalog = new();

            foreach (ParameterUiDefinition definition in catalog.GetAll())
            {
                Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));

                Assert.False(string.IsNullOrWhiteSpace(definition.GroupName));

                Assert.False(string.IsNullOrWhiteSpace(definition.Description));
            }
        }

        [Fact]
        public void GetByPage_ShouldReturnOnlyRequestedPage()
        {
            StaticParameterUiCatalog catalog = new();

            IReadOnlyCollection<ParameterUiDefinition> operationDefinitions = catalog.GetByPage(ParameterUiPage.Operation);

            Assert.NotEmpty(operationDefinitions);

            Assert.All(operationDefinitions, definition =>
                {
                    Assert.Equal(ParameterUiPage.Operation, definition.Page);
                });

            IReadOnlyCollection<ParameterUiDefinition> configurationDefinitions = catalog.GetByPage(ParameterUiPage.Configuration);

            Assert.NotEmpty(configurationDefinitions);

            Assert.All(configurationDefinitions, definition =>
                {
                    Assert.Equal(ParameterUiPage.Configuration, definition.Page);
                });
        }
    }
}
