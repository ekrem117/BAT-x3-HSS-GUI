using BatX3_HSS_GUI.Domain.Parameters;

namespace BatX3_HSS_GUI.Application.Parameters
{
    public interface IParameterCatalog
    {
        IReadOnlyCollection<ParameterDefinition> GetAll();

        bool TryGet(string parameterName, out ParameterDefinition? definition);

        ParameterDefinition GetRequired(string parameterName);
    }
}