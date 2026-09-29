using BatX3_HSS_GUI.Application.Actions;
using BatX3_HSS_GUI.Application.Parameters;
using BatX3_HSS_GUI.Application.Parameters.UI;
using BatX3_HSS_GUI.Client.ViewModels.Actions;
using BatX3_HSS_GUI.Domain.Parameters;
using System.Collections.ObjectModel;

namespace BatX3_HSS_GUI.Client.ViewModels.Parameters
{

    public sealed class ParametersViewModel
    {
        private readonly IReadOnlyDictionary<
            string,
            ParameterItemViewModel> _parametersByName;

        private readonly IParameterService
            _parameterService;

        private bool _configurationLoaded;
        private bool _configurationLoadInProgress;

        public ParametersViewModel(
            IParameterCatalog parameterCatalog,
            IParameterUiCatalog uiCatalog,
            IParameterService parameterService,
            IApplicationActionCatalog actionCatalog,
            IApplicationActionService actionService)
        {
            _parameterService =
                parameterService;

            List<ParameterItemViewModel> parameters =
                new();

            foreach (
                ParameterUiDefinition uiDefinition
                in uiCatalog.GetAll())
            {
                ParameterDefinition definition =
                    parameterCatalog.GetRequired(
                        uiDefinition.ParameterName);

                parameters.Add(
                    new ParameterItemViewModel(
                        definition,
                        uiDefinition,
                        parameterService));
            }

            Parameters =
                new ObservableCollection<
                    ParameterItemViewModel>(
                    parameters);

            Actions =
                new ObservableCollection<
                    ApplicationActionViewModel>(
                    actionCatalog
                        .GetAll()
                        .Select(
                            definition =>
                                new ApplicationActionViewModel(
                                    definition,
                                    actionService)));

            _parametersByName =
                Parameters.ToDictionary(
                    parameter =>
                        parameter.ParameterName,
                    StringComparer.Ordinal);

            EnemyColorParameters =
                CreateConfigurationGroup(
                    ParameterUiSections.EnemyColorCalibration);

            FriendColorParameters =
                CreateConfigurationGroup(
                    ParameterUiSections.FriendColorCalibration);

            ControlParameters =
                CreateConfigurationGroup(
                    ParameterUiSections.Control);

            ConfigurationParameters =
                EnemyColorParameters
                    .Concat(
                        FriendColorParameters)
                    .Concat(
                        ControlParameters)
                    .OrderBy(
                        parameter =>
                            parameter.UiDefinition.GroupName,
                        StringComparer.CurrentCulture)
                    .ThenBy(
                        parameter =>
                            parameter.UiDefinition.DisplayOrder)
                    .ToArray();
        }

        public ObservableCollection<
            ParameterItemViewModel> Parameters
        {
            get;
        }

        public ObservableCollection<
            ApplicationActionViewModel> Actions
        {
            get;
        }

        public IReadOnlyList<
            ParameterItemViewModel> EnemyColorParameters
        {
            get;
        }

        public IReadOnlyList<
            ParameterItemViewModel> FriendColorParameters
        {
            get;
        }

        public IReadOnlyList<
            ParameterItemViewModel> ControlParameters
        {
            get;
        }

        public IReadOnlyList<
            ParameterItemViewModel> ConfigurationParameters
        {
            get;
        }

        // ============================================================
        // INITIAL CONFIGURATION LOAD
        // ============================================================

        public async Task LoadConfigurationOnceAsync(
            CancellationToken cancellationToken = default)
        {
            if (_configurationLoaded ||
                _configurationLoadInProgress)
            {
                return;
            }

            if (ConfigurationParameters.Count == 0)
            {
                _configurationLoaded =
                    true;

                return;
            }

            _configurationLoadInProgress =
                true;

            try
            {
                string[] parameterNames =
                    ConfigurationParameters
                        .Select(
                            parameter =>
                                parameter.ParameterName)
                        .ToArray();

                IReadOnlyDictionary<
                    string,
                    ParameterOperationResult> results =
                        await _parameterService.GetAsync(
                            parameterNames,
                            cancellationToken);

                bool allParametersLoaded =
                    true;

                foreach (
                    ParameterItemViewModel parameter
                    in ConfigurationParameters)
                {
                    if (!results.TryGetValue(
                            parameter.ParameterName,
                            out ParameterOperationResult result))
                    {
                        parameter.MarkInitialReadUnavailable();

                        allParametersLoaded =
                            false;

                        continue;
                    }

                    if (!result.IsSuccess)
                    {
                        allParametersLoaded =
                            false;
                    }

                    parameter.ApplyInitialReadResult(
                        result);
                }

                /*
                 * Yalnızca tüm Configuration parametreleri başarılı
                 * şekilde okunduğunda ilk yükleme tamamlanmış sayılır.
                 *
                 * Kısmi hata varsa Configuration sayfasına sonraki
                 * girişte batched GET tekrar denenir.
                 *
                 * Kullanıcının düzenlemekte olduğu değerler
                 * ParameterItemViewModel tarafından korunur.
                 */
                _configurationLoaded =
                    allParametersLoaded;
            }
            finally
            {
                _configurationLoadInProgress =
                    false;
            }
        }

        // ============================================================
        // POLLING
        // ============================================================

        public void ApplyPollingResults(
            IReadOnlyDictionary<
                string,
                ParameterOperationResult> results)
        {
            foreach (
                KeyValuePair<
                    string,
                    ParameterOperationResult> result
                in results)
            {
                if (!_parametersByName.TryGetValue(
                        result.Key,
                        out ParameterItemViewModel? parameter))
                {
                    continue;
                }

                parameter.ApplyPollingResult(
                    result.Value);
            }
        }

        // ============================================================
        // GROUPING
        // ============================================================

        private IReadOnlyList<
            ParameterItemViewModel> CreateConfigurationGroup(
                string groupName)
        {
            return Parameters
                .Where(
                    parameter =>
                        parameter.UiDefinition.Page ==
                            ParameterUiPage.Configuration &&
                        string.Equals(
                            parameter.UiDefinition.GroupName,
                            groupName,
                            StringComparison.Ordinal))
                .OrderBy(
                    parameter =>
                        parameter.UiDefinition.DisplayOrder)
                .ToArray();
        }
    }
}