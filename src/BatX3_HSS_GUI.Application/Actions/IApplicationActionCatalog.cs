namespace BatX3_HSS_GUI.Application.Actions
{
    public interface IApplicationActionCatalog
    {
        IReadOnlyCollection<ApplicationActionDefinition> GetAll();

        bool TryGet(string actionId, out ApplicationActionDefinition? definition);

        ApplicationActionDefinition GetRequired(string actionId);
    }
}