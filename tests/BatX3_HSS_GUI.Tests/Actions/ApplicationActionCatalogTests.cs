using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Tests.Actions
{
    public sealed class ApplicationActionCatalogTests
    {
        [Fact]
        public void WeaponFire_ShouldTargetWriteOnlyTriggerParameter()
        {
            StaticApplicationActionCatalog actionCatalog = new();
            StaticParameterCatalog parameterCatalog = new();

            ApplicationActionDefinition action = actionCatalog.GetRequired(ApplicationActionIds.WeaponFire);

            ParameterDefinition parameter = parameterCatalog.GetRequired(action.ParameterName);

            Assert.Equal(ParameterNames.Weapon.Fire, action.ParameterName);

            Assert.Equal(1, action.Value);

            Assert.True(action.AllowKeyboardShortcut);

            Assert.Equal(ParameterAccessType.WriteOnly, parameter.AccessType);

            Assert.True(parameter.IsTrigger);
        }

        [Fact]
        public void WeaponFire_ShouldRemainTriggerAction()
        {
            StaticApplicationActionCatalog catalog = new();

            ApplicationActionDefinition definition = catalog.GetRequired(ApplicationActionIds.WeaponFire);

            Assert.Equal(ApplicationActionIds.WeaponFire, definition.Id);

            Assert.Equal("Ateşle", definition.DisplayName);

            Assert.Equal(ParameterNames.Weapon.Fire, definition.ParameterName);

            Assert.Equal(1, definition.Value);

            Assert.Null(definition.ReleaseValue);

            Assert.Equal(ApplicationActionInteractionType.Trigger, definition.InteractionType);

            Assert.True(definition.AllowKeyboardShortcut);
        }

        [Theory]
        [InlineData(ApplicationActionIds.MotionUp, ParameterNames.Motion.ManualUp)]
        [InlineData(ApplicationActionIds.MotionDown, ParameterNames.Motion.ManualDown)]
        [InlineData(ApplicationActionIds.MotionLeft, ParameterNames.Motion.ManualLeft)]
        [InlineData(ApplicationActionIds.MotionRight, ParameterNames.Motion.ManualRight)]
        public void MotionActions_ShouldBeMomentary(string actionId, string parameterName)
        {
            StaticApplicationActionCatalog catalog = new();

            ApplicationActionDefinition definition = catalog.GetRequired(actionId);

            Assert.Equal(ApplicationActionInteractionType.Momentary, definition.InteractionType);

            Assert.Equal(parameterName, definition.ParameterName);

            Assert.Equal(1, definition.Value);

            Assert.Equal(0, definition.ReleaseValue);

            Assert.True(definition.AllowKeyboardShortcut);
        }

        [Fact]
        public void Actions_ShouldHaveUniqueIds()
        {
            StaticApplicationActionCatalog catalog = new();

            string[] duplicates = catalog
                    .GetAll()
                    .GroupBy(action => action.Id, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToArray();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void AllActions_ShouldReferenceKnownParameters()
        {
            StaticApplicationActionCatalog actionCatalog = new();

            StaticParameterCatalog parameterCatalog = new();

            foreach (ApplicationActionDefinition action in actionCatalog.GetAll())
            {
                Assert.True(parameterCatalog.TryGet(action.ParameterName, out _), $"Action bilinmeyen parametreyi kullanıyor: {action.Id}");
            }
        }
    }
}