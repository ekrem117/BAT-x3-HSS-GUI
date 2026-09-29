namespace BatX3_HSS_GUI.Application.Parameters.UI
{
    public interface IParameterUiCatalog
    {
        IReadOnlyCollection<ParameterUiDefinition> GetAll();

        IReadOnlyCollection<ParameterUiDefinition> GetByPage(ParameterUiPage page);

        bool TryGet(string parameterName, out ParameterUiDefinition? definition);

        ParameterUiDefinition GetRequired(string parameterName);
    }
}